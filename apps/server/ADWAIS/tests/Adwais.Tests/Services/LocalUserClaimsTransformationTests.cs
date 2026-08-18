// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Adwais.Application.Common.Access;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Security;

namespace Adwais.Tests.Services;

public class LocalUserClaimsTransformationTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly Mock<IDbContextFactory<AnalyticsDbContext>> _dbContextFactoryMock;
    private readonly LocalUserClaimsTransformation _transformation;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public LocalUserClaimsTransformationTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContextFactoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        _dbContextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(_dbOptions));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:KioskJwtIssuer"] = "ADWAIS"
            })
            .Build();
        _httpContext = new DefaultHttpContext();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextAccessorMock.Setup(accessor => accessor.HttpContext).Returns(_httpContext);
        _transformation = new LocalUserClaimsTransformation(
            _dbContextFactoryMock.Object,
            configuration,
            _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task TransformAsync_ProvisionedUserWithoutMembership_AddsNoClaims()
    {
        var userId = Guid.NewGuid();
        var subjectId = "auth0|alice";
        var name = "Alice Smith";
        var email = "alice@example.com";

        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = subjectId,
                Name = name,
                Email = email,
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }

        var principal = CreatePrincipal(subjectId, email, name);

        var result = await _transformation.TransformAsync(principal);

        Assert.False(result.HasClaim(c => c.Type == ClaimTypes.Role));
        Assert.False(result.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId));
        Assert.False(result.HasClaim(c => c.Type == AccessClaimTypes.IsPlatformAdmin));
    }

    [Fact]
    public async Task TransformAsync_UnknownUser_IsDeniedAndNotProvisioned()
    {
        var subjectId = "google-oauth2|bob";
        var name = "Bob Jones";
        var email = "bob@example.com";
        var principal = CreatePrincipal(subjectId, email, name);

        var result = await _transformation.TransformAsync(principal);

        Assert.Same(principal, result);
        Assert.False(result.HasClaim(c => c.Type == ClaimTypes.Role));

        await using var db = new AnalyticsDbContext(_dbOptions);
        Assert.False(await db.Users.AnyAsync(u => u.ExternalSubjectId == subjectId));
    }

    [Fact]
    public async Task TransformAsync_ShouldNotModifyPrincipal_WhenSubjectClaimIsMissing()
    {
        // Arrange
        var identity = new ClaimsIdentity("TestAuthentication");
        identity.AddClaim(new Claim(ClaimTypes.Name, "Anonymous Kiosk"));
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = await _transformation.TransformAsync(principal);

        // Assert
        Assert.Same(principal, result);
        Assert.False(result.HasClaim(c => c.Type == ClaimTypes.Role));
    }

    [Fact]
    public async Task TransformAsync_ShouldLinkPreProvisionedUserByEmail_WhenUserPreProvisionedWithoutSubject()
    {
        // Arrange
        var preProvisionedEmail = "pre@example.com";
        var preProvisionedRole = UserRole.Admin;
        
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                ExternalSubjectId = null,
                Name = preProvisionedEmail,
                Email = preProvisionedEmail,
                Role = preProvisionedRole
            });
            await db.SaveChangesAsync();
        }

        var subjectId = "keycloak-user-123";
        var nameClaim = "Pre Linked User";
        var principal = CreatePrincipal(subjectId, preProvisionedEmail, nameClaim);

        // Act
        var result = await _transformation.TransformAsync(principal);

        // Assert: linking happens, but a user without membership gets no claims.
        Assert.False(result.HasClaim(c => c.Type == ClaimTypes.Role));

        // Verify linked fields in DB
        await using var dbVerify = new AnalyticsDbContext(_dbOptions);
        var user = await dbVerify.Users.SingleOrDefaultAsync(u => u.ExternalSubjectId == subjectId);
        Assert.NotNull(user);
        Assert.Equal(nameClaim, user.Name);
        Assert.Equal(preProvisionedEmail, user.Email);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task TransformAsync_ShouldReconcileExistingUserByEmail_WhenExternalSubjectChanged()
    {
        var userId = Guid.NewGuid();
        const string email = "existing@example.com";

        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "old-entra-object-id",
                Name = "Existing User",
                Email = email,
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }

        var result = await _transformation.TransformAsync(
            CreatePrincipal("new-oidc-subject", email, "Updated User"));

        Assert.False(result.HasClaim(c => c.Type == ClaimTypes.Role));

        await using var dbVerify = new AnalyticsDbContext(_dbOptions);
        var users = await dbVerify.Users.ToListAsync();
        var user = Assert.Single(users);
        Assert.Equal(userId, user.Id);
        Assert.Equal("new-oidc-subject", user.ExternalSubjectId);
        Assert.Equal("Updated User", user.Name);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task TransformAsync_ShouldSyncNameAndEmail_WhenSubjectMatchesButClaimsDiffer()
    {
        // Arrange
        var subjectId = "custom-subject";
        var originalName = "Old Name";
        var originalEmail = "old@example.com";

        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                ExternalSubjectId = subjectId,
                Name = originalName,
                Email = originalEmail,
                Role = UserRole.Employee
            });
            await db.SaveChangesAsync();
        }

        var newName = "New Name";
        var newEmail = "new@example.com";
        var principal = CreatePrincipal(subjectId, newEmail, newName);

        // Act
        var result = await _transformation.TransformAsync(principal);

        // Assert
        Assert.NotNull(result);

        // Verify database updated
        await using var dbVerify = new AnalyticsDbContext(_dbOptions);
        var user = await dbVerify.Users.SingleOrDefaultAsync(u => u.ExternalSubjectId == subjectId);
        Assert.NotNull(user);
        Assert.Equal(newName, user.Name);
        Assert.Equal(newEmail, user.Email);
    }

    [Fact]
    public async Task TransformAsync_ShouldSkipKioskTokens()
    {
        var identity = new ClaimsIdentity("KioskJwt");
        identity.AddClaim(new Claim("iss", "ADWAIS"));
        identity.AddClaim(new Claim("sub", "demo-visitor"));
        identity.AddClaim(new Claim("role", "Viewer"));
        var principal = new ClaimsPrincipal(identity);

        var result = await _transformation.TransformAsync(principal);

        Assert.Same(principal, result);
        _dbContextFactoryMock.Verify(
            factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TransformAsync_OrgMembership_AddsOrgClaimAndMembershipRole()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "membership-org-user",
                Name = "Org User",
                Email = "org@example.com",
                Role = UserRole.Employee
            });
            db.UserAccesses.Add(new UserAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = orgId,
                TenantId = null,
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }

        var result = await _transformation.TransformAsync(CreatePrincipal("membership-org-user", "org@example.com", "Org User"));

        Assert.True(result.IsInRole("Admin"));
        Assert.True(result.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId && c.Value == orgId.ToString()));
        Assert.False(result.HasClaim(c => c.Type == AccessClaimTypes.TenantId));
    }

    [Fact]
    public async Task TransformAsync_TenantViewerMembership_AddsOrgAndTenantClaims()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "membership-tenant-user",
                Name = "Tenant User",
                Email = "tenant@example.com",
                Role = UserRole.Employee
            });
            db.UserAccesses.Add(new UserAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = orgId,
                TenantId = tenantId,
                Role = UserRole.TenantViewer
            });
            await db.SaveChangesAsync();
        }

        var result = await _transformation.TransformAsync(CreatePrincipal("membership-tenant-user", "tenant@example.com", "Tenant User"));

        Assert.True(result.IsInRole("TenantViewer"));
        Assert.True(result.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId && c.Value == orgId.ToString()));
        Assert.True(result.HasClaim(c => c.Type == AccessClaimTypes.TenantId && c.Value == tenantId.ToString()));
    }

    [Fact]
    public async Task TransformAsync_PlatformAdminMembership_AddsNoOrgClaim()
    {
        var userId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "membership-platform-user",
                Name = "Platform User",
                Email = "platform@example.com",
                Role = UserRole.Employee
            });
            db.UserAccesses.Add(new UserAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = null,
                TenantId = null,
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }

        var result = await _transformation.TransformAsync(CreatePrincipal("membership-platform-user", "platform@example.com", "Platform User"));

        Assert.True(result.IsInRole("Admin"));
        Assert.True(result.HasClaim(c => c.Type == AccessClaimTypes.IsPlatformAdmin && c.Value == "true"));
        Assert.False(result.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId));
        Assert.False(result.HasClaim(c => c.Type == AccessClaimTypes.TenantId));
    }

    [Fact]
    public async Task TransformAsync_MembershipRolesReplaceLegacyRole()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "membership-role-user",
                Name = "Role User",
                Email = "role@example.com",
                Role = UserRole.Employee
            });
            db.UserAccesses.Add(new UserAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = orgId,
                TenantId = null,
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }

        var result = await _transformation.TransformAsync(CreatePrincipal("membership-role-user", "role@example.com", "Role User"));

        Assert.True(result.IsInRole("Admin"));
        Assert.False(result.IsInRole("Employee"));
    }

    [Fact]
    public async Task TransformAsync_AdminRequestingOrgHeader_EmitsOrgClaimsWithAdminRole()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "scope-admin-user",
                Name = "Scope Admin",
                Email = "scope-admin@example.com",
                Role = UserRole.Employee
            });
            db.UserAccesses.Add(new UserAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = null,
                TenantId = null,
                Role = UserRole.Admin
            });
            await db.SaveChangesAsync();
        }

        _httpContext.Request.Headers[AccessRequestHeaders.OrganizationId] = orgId.ToString();

        var result = await _transformation.TransformAsync(CreatePrincipal("scope-admin-user", "scope-admin@example.com", "Scope Admin"));

        Assert.True(result.IsInRole("Admin"));
        Assert.True(result.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId && c.Value == orgId.ToString()));
        Assert.False(result.HasClaim(c => c.Type == AccessClaimTypes.IsPlatformAdmin));
    }

    [Fact]
    public async Task TransformAsync_OrgMemberRequestingOtherOrg_AddsNoClaims()
    {
        var userId = Guid.NewGuid();
        var ownOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "scope-member-user",
                Name = "Scope Member",
                Email = "scope-member@example.com",
                Role = UserRole.Employee
            });
            db.UserAccesses.Add(new UserAccess
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrganizationId = ownOrg,
                TenantId = null,
                Role = UserRole.Employee
            });
            await db.SaveChangesAsync();
        }

        _httpContext.Request.Headers[AccessRequestHeaders.OrganizationId] = otherOrg.ToString();

        var result = await _transformation.TransformAsync(CreatePrincipal("scope-member-user", "scope-member@example.com", "Scope Member"));

        Assert.False(result.HasClaim(c => c.Type == ClaimTypes.Role));
        Assert.False(result.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId));
    }

    [Fact]
    public async Task TransformAsync_MultiOrgMemberWithoutHeader_DefaultsToFirstOrg()
    {
        var userId = Guid.NewGuid();
        var firstOrg = Guid.NewGuid();
        var secondOrg = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User
            {
                Id = userId,
                ExternalSubjectId = "scope-multi-user",
                Name = "Multi User",
                Email = "multi@example.com",
                Role = UserRole.Employee
            });
            db.UserAccesses.AddRange(
                new UserAccess { Id = Guid.NewGuid(), UserId = userId, OrganizationId = firstOrg, TenantId = null, Role = UserRole.Viewer },
                new UserAccess { Id = Guid.NewGuid(), UserId = userId, OrganizationId = secondOrg, TenantId = null, Role = UserRole.Admin });
            await db.SaveChangesAsync();
        }

        var result = await _transformation.TransformAsync(CreatePrincipal("scope-multi-user", "multi@example.com", "Multi User"));

        Assert.True(result.IsInRole("Viewer"));
        Assert.False(result.IsInRole("Admin"));
        Assert.True(result.HasClaim(c => c.Type == AccessClaimTypes.OrganizationId && c.Value == firstOrg.ToString()));
    }

    private static ClaimsPrincipal CreatePrincipal(string subjectId, string email, string name)
    {
        var identity = new ClaimsIdentity("FederatedAuthentication");
        identity.AddClaim(new Claim("sub", subjectId));
        identity.AddClaim(new Claim("email", email));
        identity.AddClaim(new Claim("name", name));
        
        return new ClaimsPrincipal(identity);
    }
}

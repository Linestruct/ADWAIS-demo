// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using FluentResults;

namespace Adwais.Tests.Services;

public class UserServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<ICurrentAccess> _accessMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AnalyticsDbContext(_dbOptions);
        _accessMock = new Mock<ICurrentAccess>();
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(null, null, [UserRole.PlatformAdmin]));
        _userService = new UserService(_dbContext, _accessMock.Object);
    }

    private void GivenOrgScope(Guid organizationId)
    {
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(organizationId, null, [UserRole.Admin]));
    }

    private void GivenPlatformAdminOrgScope(Guid organizationId)
    {
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(organizationId, null, [UserRole.Admin]) { IsPlatformAdmin = true });
    }

    private void GivenDeniedScope()
    {
        _accessMock.Setup(access => access.Scope).Returns((AccessScope?)null);
    }

    private async Task<User> SeedUserAsync(string name = "User", UserRole role = UserRole.Employee)
    {
        var user = new User { Id = Guid.NewGuid(), Name = name };
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task SeedMembershipAsync(Guid userId, Guid organizationId, UserRole role = UserRole.Employee)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.UserAccesses.Add(new UserAccess
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = organizationId,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllUsers_ForPlatformAdmin()
    {
        // Arrange
        await SeedUserAsync("User One");
        await SeedUserAsync("User Two");

        // Act
        var result = await _userService.GetUsersAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnOnlyOrgMembers_ForOrgScope()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var member = await SeedUserAsync("Member");
        var outsider = await SeedUserAsync("Outsider");
        var memberless = await SeedUserAsync("Memberless");
        await SeedMembershipAsync(member.Id, orgId);
        await SeedMembershipAsync(outsider.Id, otherOrgId);

        GivenOrgScope(orgId);

        // Act
        var result = await _userService.GetUsersAsync(CancellationToken.None);

        // Assert
        var names = result.Select(user => user.Name).ToList();
        Assert.Contains("Member", names);
        Assert.DoesNotContain("Outsider", names);
        Assert.DoesNotContain("Memberless", names);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnEmpty_WhenScopeIsDenied()
    {
        // Arrange
        await SeedUserAsync("Hidden User");
        GivenDeniedScope();

        // Act
        var result = await _userService.GetUsersAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnCorrectUser_WhenExists()
    {
        // Arrange
        var user = await SeedUserAsync("Target User");

        // Act
        var result = await _userService.GetUserByIdAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Target User", result.Name);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenOutsideOrgScope()
    {
        // Arrange
        var user = await SeedUserAsync("Outsider");
        await SeedMembershipAsync(user.Id, Guid.NewGuid());

        GivenOrgScope(Guid.NewGuid());

        // Act
        var result = await _userService.GetUserByIdAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _userService.GetUserByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnOrgMember_ForOrgScope()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = await SeedUserAsync("Member");
        await SeedMembershipAsync(user.Id, orgId);

        GivenOrgScope(orgId);

        // Act
        var result = await _userService.GetUserByIdAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public void AnalyticsDbContext_ShouldHaveUniqueIndexOnUserEmail()
    {
        // Arrange & Act
        using var db = new AnalyticsDbContext(_dbOptions);
        var entityType = db.Model.FindEntityType(typeof(User));
        var emailProperty = entityType?.FindProperty(nameof(User.Email));
        var index = entityType?.GetIndexes().FirstOrDefault(i => i.Properties.Contains(emailProperty));

        // Assert
        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldAddUserToDatabase_WithEmailAsNamePlaceholder()
    {
        // Arrange
        GivenOrgScope(Guid.NewGuid());

        // Act
        var user = await _userService.CreateUserAsync("newuser@example.com", UserRole.Admin, ct: CancellationToken.None);

        // Assert
        Assert.True(user.IsSuccess);
        Assert.NotEqual(Guid.Empty, user.Value.Id);
        Assert.Equal("newuser@example.com", user.Value.Email);
        Assert.Equal("newuser@example.com", user.Value.Name);

        await using var db = new AnalyticsDbContext(_dbOptions);
        var dbUser = await db.Users.SingleOrDefaultAsync(u => u.Id == user.Value.Id);
        Assert.NotNull(dbUser);
        Assert.Equal(UserRole.Admin, (await db.UserAccesses.SingleAsync(a => a.UserId == user.Value.Id)).Role);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateMembershipInCallerOrganization_ForOrgScope()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        GivenOrgScope(orgId);

        // Act
        var user = await _userService.CreateUserAsync("member@example.com", UserRole.Viewer, ct: CancellationToken.None);

        // Assert
        await using var db = new AnalyticsDbContext(_dbOptions);
        Assert.True(user.IsSuccess);
        var membership = await db.UserAccesses.SingleOrDefaultAsync(access => access.UserId == user.Value.Id);
        Assert.NotNull(membership);
        Assert.Equal(orgId, membership.OrganizationId);
        Assert.Equal(UserRole.Viewer, membership.Role);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldFailValidation_WhenPlatformScopeHasNoOrganization()
    {
        var result = await _userService.CreateUserAsync("platform-created@example.com", UserRole.Employee, ct: CancellationToken.None);

        AssertFailure<ValidationError>(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateMembershipInExplicitOrganization_ForPlatformScope()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Organizations.Add(new Organization { Id = orgId, Name = "Target", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        // Act
        var user = await _userService.CreateUserAsync("targeted@example.com", UserRole.Viewer, orgId, CancellationToken.None);

        // Assert
        await using var dbCtx = new AnalyticsDbContext(_dbOptions);
        Assert.True(user.IsSuccess);
        var membership = await dbCtx.UserAccesses.SingleAsync(access => access.UserId == user.Value.Id);
        Assert.Equal(orgId, membership.OrganizationId);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldFailScope_WhenExplicitOrganizationIsOutsideScope()
    {
        // Arrange
        GivenOrgScope(Guid.NewGuid());

        var result = await _userService.CreateUserAsync("foreign@example.com", UserRole.Viewer, Guid.NewGuid(), CancellationToken.None);

        AssertFailure<ScopeDeniedError>(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldFailNotFound_WhenExplicitOrganizationIsMissing()
    {
        var result = await _userService.CreateUserAsync("missing@example.com", UserRole.Viewer, Guid.NewGuid(), CancellationToken.None);

        AssertFailure<NotFoundError>(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreatePlatformRow_ForPlatformAdminRole()
    {
        // Act
        var user = await _userService.CreateUserAsync("owner@example.com", UserRole.PlatformAdmin, ct: CancellationToken.None);

        // Assert
        await using var db = new AnalyticsDbContext(_dbOptions);
        Assert.True(user.IsSuccess);
        var membership = await db.UserAccesses.SingleAsync(access => access.UserId == user.Value.Id);
        Assert.Null(membership.OrganizationId);
        Assert.Equal(UserRole.PlatformAdmin, membership.Role);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldFailScope_WhenOrgScopeCreatesPlatformAdmin()
    {
        // Arrange
        GivenOrgScope(Guid.NewGuid());

        var result = await _userService.CreateUserAsync("escalation@example.com", UserRole.PlatformAdmin, ct: CancellationToken.None);

        AssertFailure<ScopeDeniedError>(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreatePlatformRow_WhenPlatformAdminWearsOrganizationScope()
    {
        GivenPlatformAdminOrgScope(Guid.NewGuid());

        var user = await _userService.CreateUserAsync("platform-in-org-view@example.com", UserRole.PlatformAdmin, ct: CancellationToken.None);

        Assert.True(user.IsSuccess);
        await using var db = new AnalyticsDbContext(_dbOptions);
        var membership = await db.UserAccesses.SingleAsync(access => access.UserId == user.Value.Id);
        Assert.Null(membership.OrganizationId);
        Assert.Equal(UserRole.PlatformAdmin, membership.Role);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldFailValidation_WhenPlatformAdminTargetsOrganization()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Organizations.Add(new Organization { Id = orgId, Name = "Target", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var result = await _userService.CreateUserAsync("confused@example.com", UserRole.PlatformAdmin, orgId, CancellationToken.None);

        AssertFailure<ValidationError>(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldFailValidation_ForTenantViewerRole()
    {
        // Arrange
        GivenOrgScope(Guid.NewGuid());

        var result = await _userService.CreateUserAsync("viewer@example.com", UserRole.TenantViewer, ct: CancellationToken.None);

        AssertFailure<ValidationError>(result);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldFailScope_WhenScopeIsDenied()
    {
        // Arrange
        GivenDeniedScope();

        var result = await _userService.CreateUserAsync("denied@example.com", UserRole.Admin, ct: CancellationToken.None);

        AssertFailure<ScopeDeniedError>(result);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldModifyUser_WhenExists()
    {
        // Arrange
        var user = await SeedUserAsync("Original Name");

        // Act
        var result = await _userService.UpdateUserAsync(user.Id, "Updated Name", UserRole.Admin, CancellationToken.None);

// Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldUpdateScopedMembershipRole_ForOrgScope()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = await SeedUserAsync("Member");
        await SeedMembershipAsync(user.Id, orgId, UserRole.Employee);

        GivenOrgScope(orgId);

        // Act
        var result = await _userService.UpdateUserAsync(user.Id, null, UserRole.Viewer, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        await using var db = new AnalyticsDbContext(_dbOptions);
        var membership = await db.UserAccesses.SingleAsync(access => access.UserId == user.Id && access.OrganizationId == orgId);
        Assert.Equal(UserRole.Viewer, membership.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldFailScope_WhenOutsideOrgScope()
    {
        // Arrange
        var user = await SeedUserAsync("Outsider");
        await SeedMembershipAsync(user.Id, Guid.NewGuid());

        GivenOrgScope(Guid.NewGuid());

        // Act
        var result = await _userService.UpdateUserAsync(user.Id, "New Name", null, CancellationToken.None);

        // Assert
        AssertFailure<ScopeDeniedError>(result);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        var unchanged = await verifyDb.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal("Outsider", unchanged.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldFailNotFound_WhenNotExists()
    {
        // Act
        var result = await _userService.UpdateUserAsync(Guid.NewGuid(), "Name", UserRole.Employee, CancellationToken.None);

        // Assert
        AssertFailure<NotFoundError>(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveUser_WhenExists()
    {
        // Arrange
        var user = await SeedUserAsync("To Delete");

        // Act
        var result = await _userService.DeleteUserAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.False(await verifyDb.Users.AnyAsync(u => u.Id == user.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveOrgMember_AndMemberships_ForOrgScope()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = await SeedUserAsync("Org Member");
        await SeedMembershipAsync(user.Id, orgId);

        GivenOrgScope(orgId);

        // Act
        var result = await _userService.DeleteUserAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.False(await verifyDb.UserAccesses.AnyAsync(access => access.UserId == user.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRefuseCrossOrgUser_ForOrgScope()
    {
        // Arrange
        var homeOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var user = await SeedUserAsync("Shared Member");
        await SeedMembershipAsync(user.Id, homeOrg);
        await SeedMembershipAsync(user.Id, otherOrg);

        GivenOrgScope(homeOrg);

        // Act
        var result = await _userService.DeleteUserAsync(user.Id, CancellationToken.None);

        // Assert
        AssertFailure<ScopeDeniedError>(result);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.True(await verifyDb.Users.AnyAsync(u => u.Id == user.Id));
        Assert.Equal(2, await verifyDb.UserAccesses.CountAsync(access => access.UserId == user.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldFailNotFound_WhenNotExists()
    {
        // Act
        var result = await _userService.DeleteUserAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        AssertFailure<NotFoundError>(result);
    }

    [Fact]
    public async Task GetUserByExternalSubjectIdAsync_ShouldReturnCorrectUser_WhenExists()
    {
        // Arrange
        var subjectId = "auth0|user-123";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "OIDC User" };
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        var result = await _userService.GetUserByExternalSubjectIdAsync(subjectId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subjectId, result.ExternalSubjectId);
        Assert.Equal("OIDC User", result.Name);
    }

    [Fact]
    public async Task GetUserByExternalSubjectIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _userService.GetUserByExternalSubjectIdAsync("missing-subject", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserMembershipsAsync_PlatformAdmin_ReturnsAllMembershipRows()
    {
        // Arrange
        var user = await SeedUserAsync("Member");
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        await SeedMembershipAsync(user.Id, orgA, UserRole.Admin);
        await SeedMembershipAsync(user.Id, orgB, UserRole.Employee);

        // Act
        var memberships = (await _userService.GetUserMembershipsAsync(user.Id, CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, memberships.Count);
        Assert.Contains(memberships, m => m.OrganizationId == orgA && m.Role == UserRole.Admin);
        Assert.Contains(memberships, m => m.OrganizationId == orgB && m.Role == UserRole.Employee);
    }

    [Fact]
    public async Task GetUserMembershipsAsync_OrgScope_ReturnsOnlyCallerOrganizationRows()
    {
        // Arrange
        var ownOrg = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var user = await SeedUserAsync("Member");
        await SeedMembershipAsync(user.Id, ownOrg);
        await SeedMembershipAsync(user.Id, otherOrg);
        GivenOrgScope(ownOrg);

        // Act
        var memberships = (await _userService.GetUserMembershipsAsync(user.Id, CancellationToken.None)).ToList();

        // Assert
        Assert.Single(memberships);
        Assert.Equal(ownOrg, memberships[0].OrganizationId);
    }

    [Fact]
    public async Task GetUserMembershipsAsync_DeniedScope_ReturnsEmpty()
    {
        // Arrange
        var user = await SeedUserAsync("Member");
        await SeedMembershipAsync(user.Id, Guid.NewGuid());
        GivenDeniedScope();

        // Act
        var memberships = await _userService.GetUserMembershipsAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.Empty(memberships);
    }

    [Fact]
    public async Task AddUserMembershipAsync_OrgAdmin_AddsMembershipInCallerOrganization()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = await SeedUserAsync("New Member");
        _dbContext.Organizations.Add(new Organization { Id = orgId, Name = "Org" });
        _dbContext.SaveChanges();
        GivenOrgScope(orgId);

        // Act
        var membership = await _userService.AddUserMembershipAsync(user.Id, orgId, UserRole.Employee, CancellationToken.None);

        // Assert
        Assert.True(membership.IsSuccess);
        Assert.Equal(user.Id, membership.Value.UserId);
        Assert.Equal(orgId, membership.Value.OrganizationId);
        Assert.Equal(UserRole.Employee, membership.Value.Role);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.True(await verifyDb.UserAccesses.AnyAsync(a => a.UserId == user.Id && a.OrganizationId == orgId));
    }

    [Fact]
    public async Task AddUserMembershipAsync_OrgAdmin_TargetingOtherOrganization_FailsScope()
    {
        // Arrange
        var user = await SeedUserAsync("New Member");
        GivenOrgScope(Guid.NewGuid());

        var result = await _userService.AddUserMembershipAsync(user.Id, Guid.NewGuid(), UserRole.Employee, CancellationToken.None);

        AssertFailure<ScopeDeniedError>(result);
    }

    [Fact]
    public async Task AddUserMembershipAsync_OrgAdmin_CreatingPlatformRow_FailsScope()
    {
        // Arrange
        var user = await SeedUserAsync("New Member");
        GivenOrgScope(Guid.NewGuid());

        var result = await _userService.AddUserMembershipAsync(user.Id, null, UserRole.PlatformAdmin, CancellationToken.None);

        AssertFailure<ScopeDeniedError>(result);
    }

    [Fact]
    public async Task AddUserMembershipAsync_PlatformAdmin_AddsMembershipInAnyOrganization()
    {
        // Arrange
        var targetOrg = Guid.NewGuid();
        var user = await SeedUserAsync("New Member");
        _dbContext.Organizations.Add(new Organization { Id = targetOrg, Name = "Org" });
        await _dbContext.SaveChangesAsync();

        // Act
        var membership = await _userService.AddUserMembershipAsync(user.Id, targetOrg, UserRole.Admin, CancellationToken.None);

        // Assert
        Assert.True(membership.IsSuccess);
        Assert.Equal(targetOrg, membership.Value.OrganizationId);
    }

    [Fact]
    public async Task AddUserMembershipAsync_PlatformAdmin_AddsPlatformAdminRow()
    {
        // Arrange
        var user = await SeedUserAsync("New Platform Admin");

        // Act
        var membership = await _userService.AddUserMembershipAsync(user.Id, null, UserRole.PlatformAdmin, CancellationToken.None);

        // Assert
        Assert.True(membership.IsSuccess);
        Assert.Null(membership.Value.OrganizationId);
        Assert.Equal(UserRole.PlatformAdmin, membership.Value.Role);
    }

    [Fact]
    public async Task AddUserMembershipAsync_PlatformAdmin_UnknownOrganization_FailsNotFound()
    {
        // Arrange
        var user = await SeedUserAsync("New Member");

        var result = await _userService.AddUserMembershipAsync(user.Id, Guid.NewGuid(), UserRole.Employee, CancellationToken.None);

        AssertFailure<NotFoundError>(result);
    }

    [Fact]
    public async Task AddUserMembershipAsync_UnknownUser_FailsNotFound()
    {
        var result = await _userService.AddUserMembershipAsync(Guid.NewGuid(), Guid.NewGuid(), UserRole.Employee, CancellationToken.None);

        AssertFailure<NotFoundError>(result);
    }

    [Fact]
    public async Task AddUserMembershipAsync_TenantViewerRole_FailsValidation()
    {
        // Arrange
        var user = await SeedUserAsync("New Viewer");

        var result = await _userService.AddUserMembershipAsync(user.Id, Guid.NewGuid(), UserRole.TenantViewer, CancellationToken.None);

        AssertFailure<ValidationError>(result);
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_PlatformAdmin_RemovesRow()
    {
        // Arrange
        var user = await SeedUserAsync("Member");
        var membershipId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.UserAccesses.Add(new UserAccess
            {
                Id = membershipId,
                UserId = user.Id,
                OrganizationId = Guid.NewGuid(),
                Role = UserRole.Employee,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act
        var removed = await _userService.RemoveUserMembershipAsync(user.Id, membershipId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(removed.IsSuccess);
        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.False(await verifyDb.UserAccesses.AnyAsync(a => a.Id == membershipId));
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_OrgAdmin_RemovesOwnOrganizationRow()
    {
        // Arrange
        var ownOrg = Guid.NewGuid();
        var user = await SeedUserAsync("Member");
        var membershipId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.UserAccesses.Add(new UserAccess
            {
                Id = membershipId,
                UserId = user.Id,
                OrganizationId = ownOrg,
                Role = UserRole.Employee,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        GivenOrgScope(ownOrg);

        // Act
        var removed = await _userService.RemoveUserMembershipAsync(user.Id, membershipId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(removed.IsSuccess);
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_OrgAdmin_OtherOrganizationRow_FailsScope()
    {
        // Arrange
        var otherOrg = Guid.NewGuid();
        var user = await SeedUserAsync("Member");
        var membershipId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.UserAccesses.Add(new UserAccess
            {
                Id = membershipId,
                UserId = user.Id,
                OrganizationId = otherOrg,
                Role = UserRole.Employee,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        GivenOrgScope(Guid.NewGuid());

        // Act
        var removed = await _userService.RemoveUserMembershipAsync(user.Id, membershipId, Guid.NewGuid(), CancellationToken.None);

        // Assert
        AssertFailure<ScopeDeniedError>(removed);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.True(await verifyDb.UserAccesses.AnyAsync(a => a.Id == membershipId));
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_SelfPlatformAdminRow_FailsScope()
    {
        // Arrange
        var platformUserId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User { Id = platformUserId, Name = "Platform Admin" });
            db.UserAccesses.Add(new UserAccess
            {
                Id = membershipId,
                UserId = platformUserId,
                OrganizationId = null,
                Role = UserRole.PlatformAdmin,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var result = await _userService.RemoveUserMembershipAsync(platformUserId, membershipId, platformUserId, CancellationToken.None);

        AssertFailure<ScopeDeniedError>(result);
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_UnknownMembership_FailsNotFound()
    {
        // Arrange
        var user = await SeedUserAsync("Member");

        // Act
        var removed = await _userService.RemoveUserMembershipAsync(user.Id, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        AssertFailure<NotFoundError>(removed);
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_DeniedScope_FailsScope()
    {
        // Arrange
        var user = await SeedUserAsync("Member");
        GivenDeniedScope();

        // Act
        var removed = await _userService.RemoveUserMembershipAsync(user.Id, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        AssertFailure<ScopeDeniedError>(removed);
    }

    private static void AssertFailure<TError>(Result result) where TError : IError
    {
        Assert.True(result.IsFailed);
        Assert.IsType<TError>(Assert.Single(result.Errors));
    }

    private static void AssertFailure<TError>(Result<User> result) where TError : IError
    {
        Assert.True(result.IsFailed);
        Assert.IsType<TError>(Assert.Single(result.Errors));
    }

    private static void AssertFailure<TError>(Result<UserAccess> result) where TError : IError
    {
        Assert.True(result.IsFailed);
        Assert.IsType<TError>(Assert.Single(result.Errors));
    }
}

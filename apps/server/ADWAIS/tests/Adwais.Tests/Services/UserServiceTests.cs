// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;

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
            .Returns(new AccessScope(null, null, [UserRole.Admin]));
        _userService = new UserService(_dbContext, _accessMock.Object);
    }

    private void GivenOrgScope(Guid organizationId)
    {
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(organizationId, null, [UserRole.Admin]));
    }

    private void GivenDeniedScope()
    {
        _accessMock.Setup(access => access.Scope).Returns((AccessScope?)null);
    }

    private async Task<User> SeedUserAsync(string name = "User", UserRole role = UserRole.Employee)
    {
        var user = new User { Id = Guid.NewGuid(), Name = name, Role = role };
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
        // Act
        var user = await _userService.CreateUserAsync("newuser@example.com", UserRole.Admin, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("newuser@example.com", user.Email);
        Assert.Equal("newuser@example.com", user.Name);
        Assert.Equal(UserRole.Admin, user.Role);

        await using var db = new AnalyticsDbContext(_dbOptions);
        var dbUser = await db.Users.SingleOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(dbUser);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateMembershipInCallerOrganization_ForOrgScope()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        GivenOrgScope(orgId);

        // Act
        var user = await _userService.CreateUserAsync("member@example.com", UserRole.Viewer, CancellationToken.None);

        // Assert
        await using var db = new AnalyticsDbContext(_dbOptions);
        var membership = await db.UserAccesses.SingleOrDefaultAsync(access => access.UserId == user.Id);
        Assert.NotNull(membership);
        Assert.Equal(orgId, membership.OrganizationId);
        Assert.Equal(UserRole.Viewer, membership.Role);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateMembershipInDefaultOrganization_ForPlatformAdminWithoutOrgSelection()
    {
        // Act
        var user = await _userService.CreateUserAsync("platform-created@example.com", UserRole.Employee, CancellationToken.None);

        // Assert
        await using var db = new AnalyticsDbContext(_dbOptions);
        var membership = await db.UserAccesses.SingleOrDefaultAsync(access => access.UserId == user.Id);
        Assert.NotNull(membership);
        Assert.Equal(AnalyticsDbContext.DefaultOrganizationGuid, membership.OrganizationId);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenScopeIsDenied()
    {
        // Arrange
        GivenDeniedScope();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _userService.CreateUserAsync("denied@example.com", UserRole.Admin, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldModifyUser_WhenExists()
    {
        // Arrange
        var user = await SeedUserAsync("Original Name");

        // Act
        var result = await _userService.UpdateUserAsync(user.Id, "Updated Name", UserRole.Admin, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(UserRole.Admin, result.Role);
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
        Assert.NotNull(result);
        Assert.Equal(UserRole.Viewer, result.Role);

        await using var db = new AnalyticsDbContext(_dbOptions);
        var membership = await db.UserAccesses.SingleAsync(access => access.UserId == user.Id && access.OrganizationId == orgId);
        Assert.Equal(UserRole.Viewer, membership.Role);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldReturnNull_WhenOutsideOrgScope()
    {
        // Arrange
        var user = await SeedUserAsync("Outsider");
        await SeedMembershipAsync(user.Id, Guid.NewGuid());

        GivenOrgScope(Guid.NewGuid());

        // Act
        var result = await _userService.UpdateUserAsync(user.Id, "New Name", null, CancellationToken.None);

        // Assert
        Assert.Null(result);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        var unchanged = await verifyDb.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal("Outsider", unchanged.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _userService.UpdateUserAsync(Guid.NewGuid(), "Name", UserRole.Employee, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldRemoveUser_WhenExists()
    {
        // Arrange
        var user = await SeedUserAsync("To Delete");

        // Act
        var result = await _userService.DeleteUserAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

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
        Assert.True(result);

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
        Assert.False(result);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.True(await verifyDb.Users.AnyAsync(u => u.Id == user.Id));
        Assert.Equal(2, await verifyDb.UserAccesses.CountAsync(access => access.UserId == user.Id));
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _userService.DeleteUserAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserByExternalSubjectIdAsync_ShouldReturnCorrectUser_WhenExists()
    {
        // Arrange
        var subjectId = "auth0|user-123";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "OIDC User", Role = UserRole.Employee };
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
        Assert.Equal(user.Id, membership.UserId);
        Assert.Equal(orgId, membership.OrganizationId);
        Assert.Equal(UserRole.Employee, membership.Role);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.True(await verifyDb.UserAccesses.AnyAsync(a => a.UserId == user.Id && a.OrganizationId == orgId));
    }

    [Fact]
    public async Task AddUserMembershipAsync_OrgAdmin_TargetingOtherOrganization_ThrowsUnauthorizedAccess()
    {
        // Arrange
        var user = await SeedUserAsync("New Member");
        GivenOrgScope(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _userService.AddUserMembershipAsync(user.Id, Guid.NewGuid(), UserRole.Employee, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserMembershipAsync_OrgAdmin_CreatingPlatformRow_ThrowsUnauthorizedAccess()
    {
        // Arrange
        var user = await SeedUserAsync("New Member");
        GivenOrgScope(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _userService.AddUserMembershipAsync(user.Id, null, UserRole.Admin, CancellationToken.None));
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
        Assert.Equal(targetOrg, membership.OrganizationId);
    }

    [Fact]
    public async Task AddUserMembershipAsync_PlatformAdmin_AddsPlatformAdminRow()
    {
        // Arrange
        var user = await SeedUserAsync("New Platform Admin");

        // Act
        var membership = await _userService.AddUserMembershipAsync(user.Id, null, UserRole.Admin, CancellationToken.None);

        // Assert
        Assert.Null(membership.OrganizationId);
        Assert.Equal(UserRole.Admin, membership.Role);
    }

    [Fact]
    public async Task AddUserMembershipAsync_PlatformAdmin_UnknownOrganization_ThrowsKeyNotFound()
    {
        // Arrange
        var user = await SeedUserAsync("New Member");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.AddUserMembershipAsync(user.Id, Guid.NewGuid(), UserRole.Employee, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserMembershipAsync_UnknownUser_ThrowsKeyNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.AddUserMembershipAsync(Guid.NewGuid(), Guid.NewGuid(), UserRole.Employee, CancellationToken.None));
    }

    [Fact]
    public async Task AddUserMembershipAsync_TenantViewerRole_ThrowsArgumentException()
    {
        // Arrange
        var user = await SeedUserAsync("New Viewer");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _userService.AddUserMembershipAsync(user.Id, Guid.NewGuid(), UserRole.TenantViewer, CancellationToken.None));
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
        Assert.True(removed);
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
        Assert.True(removed);
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_OrgAdmin_OtherOrganizationRow_ReturnsFalse()
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
        Assert.False(removed);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        Assert.True(await verifyDb.UserAccesses.AnyAsync(a => a.Id == membershipId));
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_SelfPlatformAdminRow_ThrowsUnauthorizedAccess()
    {
        // Arrange
        var platformUserId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(new User { Id = platformUserId, Name = "Platform Admin", Role = UserRole.Admin });
            db.UserAccesses.Add(new UserAccess
            {
                Id = membershipId,
                UserId = platformUserId,
                OrganizationId = null,
                Role = UserRole.Admin,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _userService.RemoveUserMembershipAsync(platformUserId, membershipId, platformUserId, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_UnknownMembership_ReturnsFalse()
    {
        // Arrange
        var user = await SeedUserAsync("Member");

        // Act
        var removed = await _userService.RemoveUserMembershipAsync(user.Id, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public async Task RemoveUserMembershipAsync_DeniedScope_ReturnsFalse()
    {
        // Arrange
        var user = await SeedUserAsync("Member");
        GivenDeniedScope();

        // Act
        var removed = await _userService.RemoveUserMembershipAsync(user.Id, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.False(removed);
    }
}

// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;
using Adwais.Api.Controllers;
using Adwais.Api.Controllers.Authentication;
using Adwais.Api.DTOs.Users;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ICurrentAccess> _accessMock;
    private readonly AnalyticsDbContext _dbContext;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _accessMock = new Mock<ICurrentAccess>();
        _accessMock.Setup(access => access.Scope).Returns((AccessScope?)null);
        _dbContext = new AnalyticsDbContext(new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _controller = new UserController(_userServiceMock.Object, _accessMock.Object, _dbContext);
    }

    [Theory]
    [InlineData(nameof(UserController.GetUsers))]
    [InlineData(nameof(UserController.GetUser))]
    public void ReadEndpointsAllowViewers(string actionName)
    {
        var action = typeof(UserController).GetMethod(actionName);
        var authorization = action?.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorization);
        Assert.Equal("KioskOrStaffAccess", authorization.Policy);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOkWithUsersList()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "Alice" },
            new User { Id = Guid.NewGuid(), Name = "Bob" }
        };

        _userServiceMock.Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetUsers(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUsers = Assert.IsAssignableFrom<IEnumerable<UserResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedUsers.Count());
        _userServiceMock.Verify(s => s.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_ShouldReturnOkWithUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Alice" };

        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUser(userId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(userId, returnedUser.Id);
        Assert.Equal("Alice", returnedUser.Name);
        _userServiceMock.Verify(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetUser(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        _userServiceMock.Verify(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreatedWithUser_WhenRequestContainsEmail()
    {
        // Arrange
        var request = new CreateUserRequestDto("new@example.com", UserRole.Employee);
        var createdUser = new User { Id = Guid.NewGuid(), Name = "new@example.com", Email = "new@example.com" };

        _userServiceMock.Setup(s => s.CreateUserAsync(request.Email, request.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.CreateUser(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(createdResult.Value);
        Assert.Equal("new@example.com", returnedUser.Name);
        Assert.Equal("new@example.com", returnedUser.Email);
        Assert.Null(returnedUser.Role);
        _userServiceMock.Verify(s => s.CreateUserAsync(request.Email, request.Role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnOkWithUpdatedUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var request = new UpdateUserRequestDto("Updated User", UserRole.Admin);
        var updatedUser = new User { Id = userId, Name = "Updated User" };
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(orgId, null, [UserRole.Admin]));
        _dbContext.UserAccesses.Add(new UserAccess
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            Role = UserRole.Employee,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        _userServiceMock.Setup(s => s.UpdateUserAsync(userId, request.Name, request.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.UpdateUser(userId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal("Updated User", returnedUser.Name);
        Assert.Equal(UserRole.Employee, returnedUser.Role);
        _userServiceMock.Verify(s => s.UpdateUserAsync(userId, request.Name, request.Role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequestDto("Updated User", UserRole.Admin);
        _userServiceMock.Setup(s => s.UpdateUserAsync(userId, request.Name, request.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.UpdateUser(userId, request, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        _userServiceMock.Verify(s => s.UpdateUserAsync(userId, request.Name, request.Role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteUser(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _userServiceMock.Verify(s => s.DeleteUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.DeleteUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteUser(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _userServiceMock.Verify(s => s.DeleteUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMe_ShouldReturnOkWithUser_WhenOidcUserExists()
    {
        // Arrange
        var subjectId = "auth0|user-123";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "OIDC User" };
        
        _userServiceMock.Setup(s => s.GetUserByExternalSubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var claims = new List<System.Security.Claims.Claim>
        {
            new("sub", subjectId)
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(user.Id, returnedUser.Id);
        Assert.Equal("OIDC User", returnedUser.Name);
        Assert.Equal(UserRole.Employee, returnedUser.Role);
    }

    [Fact]
    public async Task GetMe_ShouldReturnOkWithKioskDevice_WhenKioskRoleIsViewer()
    {
        // Arrange
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Role, "Viewer"),
            new(System.Security.Claims.ClaimTypes.Name, "Kiosk-Device-123")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(Guid.Empty, returnedUser.Id);
        Assert.Equal("Kiosk-Device-123", returnedUser.Name);
        Assert.Equal(UserRole.Viewer, returnedUser.Role);
    }

    [Fact]
    public async Task GetMe_ShouldReturnOrganizationFields_WhenScopeIsAnOrganization()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var subjectId = "auth0|org-user";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "Org User" };
        _userServiceMock.Setup(s => s.GetUserByExternalSubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _dbContext.Organizations.Add(new Organization { Id = orgId, Name = "Acme Consulting" });
        await _dbContext.SaveChangesAsync();
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(orgId, null, [UserRole.Admin]));

        var claims = new List<System.Security.Claims.Claim> { new("sub", subjectId) };
        GivenPrincipal(claims);

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(orgId, returnedUser.OrganizationId);
        Assert.Equal("Acme Consulting", returnedUser.OrganizationName);
        Assert.Null(returnedUser.TenantId);
        Assert.False(returnedUser.IsPlatformAdmin);
    }

    [Fact]
    public async Task GetMe_ShouldReturnPlatformAdminFields_WhenScopeIsPlatform()
    {
        // Arrange
        var subjectId = "auth0|platform-user";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "Platform User" };
        _userServiceMock.Setup(s => s.GetUserByExternalSubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _dbContext.UserAccesses.Add(new UserAccess
        {
            UserId = user.Id,
            OrganizationId = null,
            Role = UserRole.PlatformAdmin
        });
        await _dbContext.SaveChangesAsync();
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(null, null, [UserRole.PlatformAdmin]));

        var claims = new List<System.Security.Claims.Claim> { new("sub", subjectId) };
        GivenPrincipal(claims);

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.True(returnedUser.IsPlatformAdmin);
        Assert.Null(returnedUser.OrganizationId);
        Assert.Null(returnedUser.OrganizationName);
    }

    [Fact]
    public async Task GetMe_PlatformAdminWearingAnOrganization_KeepsPlatformStatus()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var subjectId = "auth0|wearing-user";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "Wearing User" };
        _userServiceMock.Setup(s => s.GetUserByExternalSubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _dbContext.UserAccesses.Add(new UserAccess
        {
            UserId = user.Id,
            OrganizationId = null,
            Role = UserRole.PlatformAdmin
        });
        _dbContext.Organizations.Add(new Organization { Id = orgId, Name = "Worn Org" });
        await _dbContext.SaveChangesAsync();
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(orgId, null, [UserRole.Admin]));

        var claims = new List<System.Security.Claims.Claim> { new("sub", subjectId) };
        GivenPrincipal(claims);

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.True(returnedUser.IsPlatformAdmin);
        Assert.Equal(orgId, returnedUser.OrganizationId);
        Assert.Equal("Worn Org", returnedUser.OrganizationName);
    }

    [Fact]
    public async Task GetMe_ShouldReturnTenantPin_WhenScopeIsTenantRestricted()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var subjectId = "auth0|tenant-viewer";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "Viewer" };
        _userServiceMock.Setup(s => s.GetUserByExternalSubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(orgId, tenantId, [UserRole.TenantViewer]));

        var claims = new List<System.Security.Claims.Claim> { new("sub", subjectId) };
        GivenPrincipal(claims);

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(orgId, returnedUser.OrganizationId);
        Assert.Equal(tenantId, returnedUser.TenantId);
    }

    private void GivenPrincipal(IEnumerable<System.Security.Claims.Claim> claims)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetMe_ShouldReturnOrgIdWithNullName_WhenOrganizationRowIsMissing()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var subjectId = "auth0|orphan-org";
        var user = new User { Id = Guid.NewGuid(), ExternalSubjectId = subjectId, Name = "Orphan User" };
        _userServiceMock.Setup(s => s.GetUserByExternalSubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(orgId, null, [UserRole.Admin]));

        var claims = new List<System.Security.Claims.Claim> { new("sub", subjectId) };
        GivenPrincipal(claims);

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserResponseDto>(okResult.Value);
        Assert.Equal(orgId, returnedUser.OrganizationId);
        Assert.Null(returnedUser.OrganizationName);
    }

    [Fact]
    public async Task GetUserMemberships_ReturnsOkWithRows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Member" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userServiceMock.Setup(s => s.GetUserMembershipsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new UserAccess { Id = Guid.NewGuid(), UserId = userId, OrganizationId = Guid.NewGuid(), Role = UserRole.Admin },
                new UserAccess { Id = Guid.NewGuid(), UserId = userId, OrganizationId = null, Role = UserRole.Admin }
            ]);

        // Act
        var result = await _controller.GetUserMemberships(userId, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<UserMembershipResponseDto>>(ok.Value).ToList();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal(userId, row.UserId));
    }

    [Fact]
    public async Task GetUserMemberships_UserMissing_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.GetUserMemberships(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        _userServiceMock.Verify(s => s.GetUserMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddUserMembership_ReturnsCreatedWithRow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Member" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var membership = new UserAccess { Id = Guid.NewGuid(), UserId = userId, OrganizationId = orgId, Role = UserRole.Admin };
        _userServiceMock.Setup(s => s.AddUserMembershipAsync(userId, orgId, UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        // Act
        var result = await _controller.AddUserMembership(
            userId,
            new AddUserMembershipRequestDto(orgId, UserRole.Admin),
            CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<UserMembershipResponseDto>(created.Value);
        Assert.Equal(membership.Id, dto.Id);
        Assert.Equal(orgId, dto.OrganizationId);
    }

    [Fact]
    public async Task AddUserMembership_UserMissing_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _controller.AddUserMembership(
            userId,
            new AddUserMembershipRequestDto(Guid.NewGuid(), UserRole.Employee),
            CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
        _userServiceMock.Verify(s => s.AddUserMembershipAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserMembership_ReturnsNoContent_WhenRemoved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Member" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userServiceMock.Setup(s => s.RemoveUserMembershipAsync(userId, membershipId, callerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        GivenPrincipal([new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, callerId.ToString())]);

        // Act
        var result = await _controller.DeleteUserMembership(userId, membershipId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteUserMembership_ReturnsNotFound_WhenNotRemoved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Member" };
        _userServiceMock.Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userServiceMock.Setup(s => s.RemoveUserMembershipAsync(userId, membershipId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        GivenPrincipal([new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]);

        // Act
        var result = await _controller.DeleteUserMembership(userId, membershipId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenClaimsAreInvalid()
    {
        // Arrange
        var principal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetMe(CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
}

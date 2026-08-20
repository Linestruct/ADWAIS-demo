// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;

namespace Adwais.Tests.Services;

public class UserServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly AnalyticsDbContext _dbContext;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AnalyticsDbContext(_dbOptions);
        _userService = new UserService(_dbContext);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllUsers()
    {
        // Arrange
        var user1 = new User { Id = Guid.NewGuid(), Name = "User One", Role = UserRole.Employee };
        var user2 = new User { Id = Guid.NewGuid(), Name = "User Two", Role = UserRole.Admin };
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.AddRange(user1, user2);
            await db.SaveChangesAsync();
        }

        // Act
        var result = await _userService.GetUsersAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, ((List<User>)result).Count);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnCorrectUser_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Target User", Role = UserRole.Employee };
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act
        var result = await _userService.GetUserByIdAsync(userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Target User", result.Name);
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
        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("newuser@example.com", user.Email);
        Assert.Equal("newuser@example.com", user.Name);
        Assert.Equal(UserRole.Admin, user.Role);

        // Verify in DB
        await using var db = new AnalyticsDbContext(_dbOptions);
        var dbUser = await db.Users.SingleOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(dbUser);
        Assert.Equal("newuser@example.com", dbUser.Email);
        Assert.Equal("newuser@example.com", dbUser.Name);
    }

    [Fact]
    public async Task UpdateUserAsync_ShouldModifyUser_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "Original Name", Role = UserRole.Employee };
        await using (var arrangeDb = new AnalyticsDbContext(_dbOptions))
        {
            arrangeDb.Users.Add(user);
            await arrangeDb.SaveChangesAsync();
        }

        // Act
        var result = await _userService.UpdateUserAsync(userId, "Updated Name", UserRole.Admin, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(UserRole.Admin, result.Role);

        // Verify in DB
        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        var dbUser = await verifyDb.Users.SingleOrDefaultAsync(u => u.Id == userId);
        Assert.NotNull(dbUser);
        Assert.Equal("Updated Name", dbUser.Name);
        Assert.Equal(UserRole.Admin, dbUser.Role);
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
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Name = "To Delete", Role = UserRole.Employee };
        await using (var arrangeDb = new AnalyticsDbContext(_dbOptions))
        {
            arrangeDb.Users.Add(user);
            await arrangeDb.SaveChangesAsync();
        }

        // Act
        var result = await _userService.DeleteUserAsync(userId, CancellationToken.None);

        // Assert
        Assert.True(result);

        // Verify in DB
        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        var exists = await verifyDb.Users.AnyAsync(u => u.Id == userId);
        Assert.False(exists);
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
}

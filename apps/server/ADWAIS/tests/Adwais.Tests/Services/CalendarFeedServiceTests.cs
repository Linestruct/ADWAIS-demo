// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adwais.Tests.Services;

public class CalendarFeedServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly CalendarFeedService _service;

    public CalendarFeedServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _service = new CalendarFeedService(new AnalyticsDbContext(_dbOptions));
    }

    private async Task<User> SeedFeedUserAsync(string token, Guid organizationId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Feed User",
            Email = "feed@example.com",
            CalendarFeedToken = token
        };
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.Users.Add(user);
        db.UserAccesses.Add(new UserAccess
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = organizationId,
            Role = UserRole.Employee,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return user;
    }

    private async Task SeedEventAsync(string title, Guid organizationId)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.CalendarEvents.Add(new CalendarEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = title,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            EventType = EventType.General,
            IsRecurring = false
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GenerateIcsFeedAsync_InvalidToken_ThrowsUnauthorizedAccess()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GenerateIcsFeedAsync("no-such-token", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateIcsFeedAsync_UserWithoutOrganizationScope_ThrowsUnauthorizedAccess()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Memberless",
            Email = "memberless@example.com",
            CalendarFeedToken = "token-without-scope"
        };
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.GenerateIcsFeedAsync("token-without-scope", CancellationToken.None));
    }

    [Fact]
    public async Task GenerateIcsFeedAsync_ScopesEventsToUserOrganization()
    {
        // Arrange
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        await SeedFeedUserAsync("scoped-token", orgA);
        await SeedEventAsync("Org A Standup", orgA);
        await SeedEventAsync("Org B Secret", orgB);

        // Act
        var bytes = await _service.GenerateIcsFeedAsync("scoped-token", CancellationToken.None);
        var ics = Encoding.UTF8.GetString(bytes);

        // Assert
        Assert.Contains("Org A Standup", ics);
        Assert.DoesNotContain("Org B Secret", ics);
    }
}

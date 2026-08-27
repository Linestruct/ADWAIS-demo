// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Administration;
using Adwais.Application.Common.Access;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class OrganizationsControllerTests
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<ICurrentAccess> _accessMock;
    private readonly OrganizationsController _controller;
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Guid _orgC = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public OrganizationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AnalyticsDbContext(options);
        _accessMock = new Mock<ICurrentAccess>();
        _controller = new OrganizationsController(_dbContext, _accessMock.Object);
    }

    private void GivenPlatformScope()
        => _accessMock.Setup(access => access.Scope).Returns(new AccessScope(null, null, [UserRole.Admin]));

    private void GivenOrgScope(Guid organizationId)
        => _accessMock.Setup(access => access.Scope).Returns(new AccessScope(organizationId, null, [UserRole.Admin]));

    private void GivenDeniedScope()
        => _accessMock.Setup(access => access.Scope).Returns((AccessScope?)null);

    private void GivenPrincipal(Guid nameIdentifier)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, nameIdentifier.ToString())],
                    "Test"))
            }
        };
    }

    private async Task SeedAsync()
    {
        _dbContext.Organizations.AddRange(
            new Organization { Id = _orgA, Name = "Alpha" },
            new Organization { Id = _orgB, Name = "Beta" },
            new Organization { Id = _orgC, Name = "Gamma" });
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetOrganizations_PlatformScope_ReturnsEveryOrganization()
    {
        GivenPlatformScope();
        await SeedAsync();

        var result = await _controller.GetOrganizations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orgs = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value).Cast<dynamic>().ToList();
        Assert.Equal(3, orgs.Count);
    }

    [Fact]
    public async Task GetOrganizations_OrgScope_ReturnsOnlyMembershipOrganizations()
    {
        GivenOrgScope(_orgA);
        GivenPrincipal(_userId);
        await SeedAsync();
        _dbContext.UserAccesses.AddRange(
            new UserAccess { Id = Guid.NewGuid(), UserId = _userId, OrganizationId = _orgA, Role = UserRole.Admin, CreatedAt = DateTimeOffset.UtcNow },
            new UserAccess { Id = Guid.NewGuid(), UserId = _userId, OrganizationId = _orgB, Role = UserRole.Employee, CreatedAt = DateTimeOffset.UtcNow });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetOrganizations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orgs = Assert.IsAssignableFrom<IEnumerable<Adwais.Api.DTOs.Organizations.OrganizationResponseDto>>(ok.Value).ToList();
        var ids = orgs.Select(org => org.Id).ToList();
        Assert.Contains(_orgA, ids);
        Assert.Contains(_orgB, ids);
        Assert.DoesNotContain(_orgC, ids);
    }

    [Fact]
    public async Task GetOrganizations_OrgScopeWithoutMembershipRows_FallsBackToScopeOrganization()
    {
        GivenOrgScope(_orgA);
        GivenPrincipal(_userId);
        await SeedAsync();

        var result = await _controller.GetOrganizations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orgs = Assert.IsAssignableFrom<IEnumerable<Adwais.Api.DTOs.Organizations.OrganizationResponseDto>>(ok.Value).ToList();
        Assert.Single(orgs);
        Assert.Equal(_orgA, orgs[0].Id);
    }

    [Fact]
    public async Task GetOrganizations_DeniedScope_ReturnsEmpty()
    {
        GivenDeniedScope();

        var result = await _controller.GetOrganizations(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var orgs = Assert.IsAssignableFrom<IEnumerable<Adwais.Api.DTOs.Organizations.OrganizationResponseDto>>(ok.Value).ToList();
        Assert.Empty(orgs);
    }
}

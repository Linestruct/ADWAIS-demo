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
using Adwais.Application.Interfaces;
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
    private readonly Mock<IOrganizationService> _organizationServiceMock;
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
        _organizationServiceMock = new Mock<IOrganizationService>();
        _controller = new OrganizationsController(_dbContext, _accessMock.Object, _organizationServiceMock.Object);
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

    [Fact]
    public async Task GetOrganization_PlatformScope_ReturnsAnyOrganization()
    {
        GivenPlatformScope();
        _organizationServiceMock.Setup(s => s.GetOrganizationAsync(_orgA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = _orgA, Name = "Alpha" });

        var result = await _controller.GetOrganization(_orgA, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Alpha", Assert.IsType<Adwais.Api.DTOs.Organizations.OrganizationResponseDto>(ok.Value).Name);
    }

    [Fact]
    public async Task GetOrganization_OrgScope_ReturnsOwnOrganizationOnly()
    {
        GivenOrgScope(_orgA);
        _organizationServiceMock.Setup(s => s.GetOrganizationAsync(_orgA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = _orgA, Name = "Alpha" });

        var own = await _controller.GetOrganization(_orgA, CancellationToken.None);
        Assert.IsType<OkObjectResult>(own.Result);

        var foreign = await _controller.GetOrganization(_orgB, CancellationToken.None);
        Assert.IsType<NotFoundResult>(foreign.Result);
        _organizationServiceMock.Verify(
            s => s.GetOrganizationAsync(_orgB, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrganization_ReturnsCreatedResponse()
    {
        GivenPlatformScope();
        _organizationServiceMock.Setup(s => s.CreateOrganizationAsync("NewCo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = _orgA, Name = "NewCo" });

        var result = await _controller.CreateOrganization(
            new Adwais.Api.DTOs.Organizations.CreateOrganizationRequestDto("NewCo"), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal("NewCo", Assert.IsType<Adwais.Api.DTOs.Organizations.OrganizationResponseDto>(created.Value).Name);
    }

    [Fact]
    public async Task RenameOrganization_OrgScope_RefusesForeignOrganization()
    {
        GivenOrgScope(_orgA);

        var result = await _controller.RenameOrganization(
            _orgB, new Adwais.Api.DTOs.Organizations.UpdateOrganizationRequestDto("Nope"), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        _organizationServiceMock.Verify(
            s => s.RenameOrganizationAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RenameOrganization_PlatformScope_ReturnsRenamedOrganization()
    {
        GivenPlatformScope();
        _organizationServiceMock.Setup(s => s.RenameOrganizationAsync(_orgA, "Renamed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _organizationServiceMock.Setup(s => s.GetOrganizationAsync(_orgA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Organization { Id = _orgA, Name = "Renamed" });

        var result = await _controller.RenameOrganization(
            _orgA, new Adwais.Api.DTOs.Organizations.UpdateOrganizationRequestDto("Renamed"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Renamed", Assert.IsType<Adwais.Api.DTOs.Organizations.OrganizationResponseDto>(ok.Value).Name);
    }

    [Fact]
    public async Task RenameOrganization_MissingOrganization_ReturnsNotFound()
    {
        GivenPlatformScope();
        _organizationServiceMock.Setup(s => s.RenameOrganizationAsync(_orgA, "Nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.RenameOrganization(
            _orgA, new Adwais.Api.DTOs.Organizations.UpdateOrganizationRequestDto("Nope"), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteOrganization_MapsServiceResults()
    {
        GivenPlatformScope();
        _organizationServiceMock.Setup(s => s.DeleteOrganizationAsync(_orgA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationDeleteResult.Deleted);
        _organizationServiceMock.Setup(s => s.DeleteOrganizationAsync(_orgB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationDeleteResult.NotFound);
        _organizationServiceMock.Setup(s => s.DeleteOrganizationAsync(_orgC, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationDeleteResult.RefusedDefaultOrganization);

        Assert.IsType<NoContentResult>(await _controller.DeleteOrganization(_orgA, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await _controller.DeleteOrganization(_orgB, CancellationToken.None));
        var conflict = Assert.IsType<ConflictObjectResult>(
            await _controller.DeleteOrganization(_orgC, CancellationToken.None));
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task GetOrganizationSummaries_ReturnsMappedSummaries()
    {
        GivenPlatformScope();
        _organizationServiceMock
            .Setup(s => s.GetOrganizationSummariesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrganizationSummary>
            {
                new(_orgA, "Alpha", 2, 3),
            });

        var result = await _controller.GetOrganizationSummaries(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var summaries = Assert.IsType<List<Adwais.Api.DTOs.Organizations.OrganizationSummaryResponseDto>>(ok.Value);
        var single = Assert.Single(summaries);
        Assert.Equal(_orgA, single.Id);
        Assert.Equal(2, single.MemberCount);
        Assert.Equal(3, single.MonitorCount);
    }
}

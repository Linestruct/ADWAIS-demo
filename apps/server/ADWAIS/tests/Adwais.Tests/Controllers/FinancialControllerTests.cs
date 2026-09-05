// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Api.Controllers.Analytics;
using Adwais.Api.DTOs.Financial;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Models;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class FinancialControllerTests
{
    private readonly Mock<IFinancialDistributionService> _distributionService = new();
    private readonly Mock<IReportingCalendar> _reportingCalendar = new();
    private readonly FinancialController _controller;

    public FinancialControllerTests()
    {
        _reportingCalendar
            .Setup(calendar => calendar.ResolvePeriodAsync(
                It.IsAny<Timeframe>(), It.IsAny<ComparisonType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedPeriod(
                DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow.AddDays(-30),
                stepsInPeriod: 30, isHourly: false, includeActualTime: false));

        _controller = new FinancialController(
            new Mock<IFinancialKpiService>().Object,
            new Mock<IFinancialSeriesService>().Object,
            _distributionService.Object,
            _reportingCalendar.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task GetOrderDistribution_ReturnsForbiddenForDeniedTenant()
    {
        var request = new OrderDistributionRequestDto { TenantId = Guid.NewGuid() };
        _distributionService
            .Setup(service => service.GetOrderDistributionAsync(
                It.IsAny<ResolvedPeriod>(), request.TenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<IReadOnlyList<Adwais.Application.DTOs.Financial.OrderBinDto>>(
                new ScopeDeniedError("the requested tenant", "the current scope")));

        var result = await _controller.GetOrderDistribution(request, CancellationToken.None);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, problem.StatusCode);
    }

    [Fact]
    public async Task GetOrderDistribution_ReturnsOkForVisibleTenant()
    {
        var request = new OrderDistributionRequestDto { TenantId = Guid.NewGuid() };
        _distributionService
            .Setup(service => service.GetOrderDistributionAsync(
                It.IsAny<ResolvedPeriod>(), request.TenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok<IReadOnlyList<Adwais.Application.DTOs.Financial.OrderBinDto>>([
                new("100-200 SEK", 100m, 200m, 2, 100m, 1m)
            ]));

        var result = await _controller.GetOrderDistribution(request, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<OrderBinResponseDto>>(response.Value));
    }

    [Fact]
    public async Task GetTransactionDensity_ReturnsForbiddenForDeniedTenant()
    {
        var request = new TransactionDensityRequestDto { TenantId = Guid.NewGuid() };
        _distributionService
            .Setup(service => service.GetTransactionDensityAsync(
                request.Period, request.TenantId, request.TenantTypes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<Adwais.Application.DTOs.Financial.TransactionDensityDto>(
                new ScopeDeniedError("the requested tenant", "the current scope")));

        var result = await _controller.GetTransactionDensity(request, CancellationToken.None);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, problem.StatusCode);
    }
}

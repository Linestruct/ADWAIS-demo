// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.EntityFrameworkCore;
using Adwais.Infrastructure.Services;
using Adwais.Application.Interfaces;
using Adwais.Application.DTOs.Financial.Upstream;
using Adwais.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Adwais.Tests.Services;

public class OrderIngestionServiceTests
{
    private (OrderIngestionService Service, Mock<HttpMessageHandler> HttpMock, AnalyticsDbContext DbContext) SetupTestEnvironment(string dbName)
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var dbContext = new AnalyticsDbContext(options);
        var dbContextFactoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        dbContextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(options));

        var httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(httpHandlerMock.Object);
        var orderSource = new LitiumOrderSource(httpClient);
        var loggerMock = new Mock<ILogger<OrderIngestionService>>();
        var eventServiceMock = new Mock<ISystemEventService>();

        var service = new OrderIngestionService(
            dbContextFactoryMock.Object,
            new[] { orderSource },
            loggerMock.Object,
            eventServiceMock.Object);

        return (service, httpHandlerMock, dbContext);
    }

    [Fact]
    public async Task IngestSingleOrderAsync_ExecutesWithoutThrowing()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase("TestDb_IngestSingle")
            .Options;

        var dbContextFactoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        dbContextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(options));

        var orderSource = new Mock<IOrderSource>();
        orderSource.SetupGet(source => source.Provider).Returns("litium");
        var loggerMock = new Mock<ILogger<OrderIngestionService>>();
        var eventServiceMock = new Mock<ISystemEventService>();

        IOrderIngestionService service = new OrderIngestionService(
            dbContextFactoryMock.Object,
            new[] { orderSource.Object },
            loggerMock.Object,
            eventServiceMock.Object);

        var tenantId = Guid.NewGuid();
        await using (var dbContext = new AnalyticsDbContext(options))
        {
            dbContext.Tenants.Add(new Adwais.Domain.Entities.Tenant { Id = tenantId, Name = "Test Tenant" });
            await dbContext.SaveChangesAsync();
        }

        var orderDto = new LitiumSyncResponse.LitiumOrderDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = "123",
            CreatedDate = DateTimeOffset.UtcNow,
            OrderStatus = "Confirmed"
        };

        // Act
        var exception = await Record.ExceptionAsync(() => service.IngestSingleOrderAsync(tenantId, "litium", LitiumOrderSource.Normalize(orderDto)));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Normalize_ShouldMapLitiumOrderToProviderNeutralOrder()
    {
        var id = Guid.NewGuid();
        var createdDate = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

        var result = LitiumOrderSource.Normalize(new LitiumSyncResponse.LitiumOrderDto
        {
            Id = id,
            OrderNumber = "ORDER-1",
            CreatedDate = createdDate,
            OrderStatus = "Confirmed",
            TotalValueIncludingVat = 125,
            TotalValueExcludingVat = 100,
            Currency = null
        });

        Assert.Equal(id.ToString("D"), result.ExternalId);
        Assert.Equal("ORDER-1", result.OrderNumber);
        Assert.Equal(createdDate.ToUniversalTime(), result.CreatedDate);
        Assert.Equal(Adwais.Domain.Enums.OrderState.Confirmed, result.State);
        Assert.Equal(125m, result.TotalValueIncludingVat);
        Assert.Equal(100m, result.TotalValueExcludingVat);
        Assert.Equal("UNK", result.Currency);
    }

    [Fact]
    public async Task ExecuteIngestionAsync_EmptyResponse_UpdatesTenantLifecycleAndSendsCorrectRequest()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var (service, httpMock, dbContext) = SetupTestEnvironment(dbName);

        var tenantId = Guid.NewGuid();
        var tenant = new Adwais.Domain.Entities.Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            OrderProviderSettings = "{\"endpointUrl\":\"https://example.com/litium/\",\"authorization\":\"ServiceAccount TW90YXN0aWNBZGFwdGVyOk1vdGFzdGljQWRhcHRlcg==\"}",
            CurrentlyFetching = true,
            LastSyncError = "Old Error"
        };
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        HttpRequestMessage? capturedRequest = null;
        var responseJson = @"{ ""TotalOrders"": 0, ""Orders"": [] }";

        httpMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson)
            });

        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);

        // Act
        var count = await service.ExecuteIngestionAsync(tenantId, startDate, endDate);

        // Assert
        Assert.Equal(0, count);

        // Assert Request
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);

        var sinceParam = Uri.EscapeDataString(startDate.ToString("O"));
        var untilParam = Uri.EscapeDataString(endDate.ToString("O"));
        var expectedUrl = $"https://example.com/litium/api/motasticadapter/sync?since={sinceParam}&until={untilParam}&skip=0&take=500";
        Assert.Equal(expectedUrl, capturedRequest.RequestUri!.ToString());

        Assert.True(capturedRequest.Headers.Contains("Authorization"));
        Assert.Equal("ServiceAccount TW90YXN0aWNBZGFwdGVyOk1vdGFzdGljQWRhcHRlcg==", string.Join(",", capturedRequest.Headers.GetValues("Authorization")));

        // Assert Lifecycle
        var updatedTenant = await dbContext.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId);
        Assert.False(updatedTenant.CurrentlyFetching);
        Assert.Null(updatedTenant.LastSyncError);
        Assert.NotNull(updatedTenant.LastPolled);
        Assert.Equal(startDate, updatedTenant.FetchedFrom);
        Assert.Equal(endDate, updatedTenant.FetchedUntil);
    }

    [Fact]
    public async Task ExecuteIngestionAsync_NonSuccessResponse_PropagatesExceptionAndRecordsError()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var (service, httpMock, dbContext) = SetupTestEnvironment(dbName);

        var tenantId = Guid.NewGuid();
        var tenant = new Adwais.Domain.Entities.Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            OrderProviderSettings = "{\"endpointUrl\":\"https://example.com/litium/\",\"authorization\":\"ServiceAccount TW90YXN0aWNBZGFwdGVyOk1vdGFzdGljQWRhcHRlcg==\"}",
            CurrentlyFetching = true
        };
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        httpMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));

        var startDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 1, 31, 0, 0, 0, TimeSpan.Zero);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.ExecuteIngestionAsync(tenantId, startDate, endDate));

        Assert.Contains("InternalServerError", exception.Message);

        var updatedTenant = await dbContext.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId);
        Assert.False(updatedTenant.CurrentlyFetching);
        Assert.NotNull(updatedTenant.LastSyncError);
        Assert.Contains("Failed to fetch chunk. Status: InternalServerError", updatedTenant.LastSyncError);
    }

    [Fact]
    public void ProviderSettings_MasksAuthorizationAndPreservesItDuringEndpointUpdate()
    {
        var source = new LitiumOrderSource(new HttpClient());
        const string settings = "{\"endpointUrl\":\"https://example.com\",\"authorization\":\"ServiceAccount secret\"}";

        var publicSettings = source.GetPublicSettings(settings);
        var merged = source.MergeSettings(settings, new Dictionary<string, string?> { ["endpointUrl"] = "https://next.example.com" });

        Assert.Equal("https://example.com", publicSettings["endpointUrl"]);
        Assert.DoesNotContain("authorization", publicSettings.Keys);
        Assert.Contains("authorization", source.GetConfiguredSecretKeys(settings));
        Assert.True(source.IsConfigured(merged));
        Assert.Contains("ServiceAccount secret", merged);
        Assert.Throws<ArgumentException>(() => source.MergeSettings(settings, new Dictionary<string, string?> { ["authorization"] = "configured" }));
    }

    [Fact]
    public void ProviderSettings_NullAuthorizationExplicitlyClearsIt()
    {
        var source = new LitiumOrderSource(new HttpClient());
        const string settings = "{\"endpointUrl\":\"https://example.com\",\"authorization\":\"ServiceAccount secret\"}";

        var cleared = source.MergeSettings(settings, new Dictionary<string, string?> { ["authorization"] = null });

        Assert.Equal("https://example.com", source.GetPublicSettings(cleared)["endpointUrl"]);
        Assert.DoesNotContain("authorization", source.GetConfiguredSecretKeys(cleared));
        Assert.False(source.IsConfigured(cleared));
    }
}

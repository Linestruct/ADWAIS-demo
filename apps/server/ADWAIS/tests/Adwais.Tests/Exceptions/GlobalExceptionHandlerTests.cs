// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Exceptions;
using Adwais.Application.Common.Exceptions;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Adwais.Tests.Exceptions;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<ISystemEventService> _eventServiceMock;
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _eventServiceMock = new Mock<ISystemEventService>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.SetupGet(scope => scope.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(factory => factory.CreateScope()).Returns(scopeMock.Object);
        serviceProviderMock.Setup(provider => provider.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);
        serviceProviderMock.Setup(provider => provider.GetService(typeof(ISystemEventService))).Returns(_eventServiceMock.Object);

        _handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, serviceProviderMock.Object);
    }

    [Fact]
    public async Task TryHandleAsync_MapsOnlyDeliberateHttpContractExceptionsToClientErrors()
    {
        var cases = new (Exception Exception, int ExpectedStatus)[]
        {
            (new HttpContractException(403, "Forbidden", "denied"), StatusCodes.Status403Forbidden),
            (new HttpContractException(404, "Not Found", "missing entity"), StatusCodes.Status404NotFound),
            (new ConfigurationException("misconfigured"), StatusCodes.Status500InternalServerError),
            (new ArgumentException("bad argument"), StatusCodes.Status500InternalServerError),
            (new InvalidOperationException("bad operation"), StatusCodes.Status500InternalServerError),
            (new KeyNotFoundException("missing entity"), StatusCodes.Status500InternalServerError),
            (new UnauthorizedAccessException("denied"), StatusCodes.Status500InternalServerError),
            (new HttpRequestException("downstream missing", null, HttpStatusCode.NotFound), StatusCodes.Status500InternalServerError),
            (new Exception("unexpected"), StatusCodes.Status500InternalServerError)
        };

        foreach (var (exception, expectedStatus) in cases)
        {
            var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

            var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

            Assert.True(handled);
            Assert.Equal(expectedStatus, httpContext.Response.StatusCode);
        }

        _eventServiceMock.Verify(
            service => service.LogErrorAsync(
                "GlobalExceptionHandler",
                It.IsAny<string>(),
                It.IsAny<Exception>(),
                null),
            Times.Exactly(cases.Length - 2));
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsFalse_IsNeverExpected_ButStillProducesProblemDetails()
    {
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var handled = await _handler.TryHandleAsync(httpContext, new InvalidOperationException("bad"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);
        Assert.StartsWith("application/json", httpContext.Response.ContentType);
    }
}

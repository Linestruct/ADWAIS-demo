// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Observability;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Adwais.Api.Extensions;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        var endpoint = configuration["OpenTelemetry:OtlpEndpoint"]
            ?? configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        var serviceVersion = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";

        var openTelemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(ObservabilityTelemetry.ServiceName, serviceVersion: serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment.name", environment.EnvironmentName)
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(ObservabilityTelemetry.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out var otlpEndpoint))
                    tracing.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(ObservabilityTelemetry.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out var otlpEndpoint))
                    metrics.AddOtlpExporter(options => options.Endpoint = otlpEndpoint);
            });

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out var otlpEndpoint))
                    options.AddOtlpExporter(exporter => exporter.Endpoint = otlpEndpoint);
            });
        });

        // Keep the local variable so the conditional exporter configuration is
        // obvious at the composition root; the builder is intentionally not
        // exposed to application code.
        _ = openTelemetry;
        return services;
    }
}

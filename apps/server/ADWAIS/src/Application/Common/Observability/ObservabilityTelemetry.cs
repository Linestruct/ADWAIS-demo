// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Adwais.Application.Common.Observability;

/// <summary>
/// The application's small, stable telemetry surface. OpenTelemetry listens
/// to these instruments when configured; the application does not depend on an
/// exporter being present.
/// </summary>
public static class ObservabilityTelemetry
{
    public const string ServiceName = "Adwais.Api";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName);

    public static readonly Counter<long> PipelineAttempts =
        Meter.CreateCounter<long>("adwais.pipeline.attempts", description: "Pipeline execution attempts.");
    public static readonly Counter<long> PipelineOutcomes =
        Meter.CreateCounter<long>("adwais.pipeline.outcomes", description: "Completed pipeline outcomes.");
    public static readonly Histogram<double> PipelineDuration =
        Meter.CreateHistogram<double>("adwais.pipeline.duration", unit: "s", description: "Pipeline execution duration.");
    public static readonly Counter<long> ProviderOutcomes =
        Meter.CreateCounter<long>("adwais.provider.outcomes", description: "Provider request outcomes.");
}

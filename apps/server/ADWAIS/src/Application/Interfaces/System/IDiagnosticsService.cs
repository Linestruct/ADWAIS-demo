// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.System;
using Adwais.Domain.Entities;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IDiagnosticsService
{
    Task<Result<OrganizationDiagnosticsDto>> GetOrganizationPipelinesAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<OrganizationDiagnosticsDto>>> GetPlatformPipelinesAsync(
        Guid? organizationId = null,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<PipelineRunDto>>> GetRunsAsync(
        Guid? organizationId,
        Guid? tenantId,
        int take,
        CancellationToken ct = default);

    Task<Result<PipelineRunDetailsDto>> GetRunAsync(
        Guid? organizationId,
        Guid runId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<DiagnosticEventDto>>> GetEventsAsync(
        Guid? organizationId,
        Guid? tenantId,
        int take,
        SystemEventLevel? minLevel,
        CancellationToken ct = default);

    Task<Result<PlatformDiagnosticsDto>> GetPlatformDiagnosticsAsync(CancellationToken ct = default);
}

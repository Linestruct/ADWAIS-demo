// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.Integrations;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Integrations;

/// <summary>
/// Describes the order and monitoring provider integrations registered with the application.
/// </summary>
[ApiController]
[Route("api/integrations")]
[Authorize(Policy = "KioskOrStaffAccess")]
public class IntegrationController(
    IEnumerable<IOrderSource> orderSources,
    IEnumerable<IMonitoringProvider> monitoringProviders) : ControllerBase
{
    /// <summary>
    /// Lists the order provider integrations available for tenant configuration.
    /// </summary>
    /// <returns>The registered order providers and their setting definitions.</returns>
    [HttpGet("order-providers")]
    public ActionResult<IEnumerable<ProviderDescriptor>> GetOrderProviders()
        => Ok(orderSources.Select(source => source.Configuration));

    /// <summary>
    /// Lists the monitoring provider integrations available for monitor configuration.
    /// </summary>
    /// <returns>The registered monitoring providers and their setting definitions.</returns>
    [HttpGet("monitoring-providers")]
    public ActionResult<IEnumerable<ProviderDescriptor>> GetMonitoringProviders()
        => Ok(monitoringProviders.Select(provider => provider.Configuration));
}

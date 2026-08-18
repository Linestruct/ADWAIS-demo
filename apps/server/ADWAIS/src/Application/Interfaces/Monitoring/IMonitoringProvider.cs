// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Monitoring.Upstream;
using Adwais.Application.DTOs.Integrations;

namespace Adwais.Application.Interfaces;

public interface IMonitoringProvider
{
    string Provider { get; }
    ProviderDescriptor Configuration { get; }
    bool IsConfigured(string? settings);
    IReadOnlyDictionary<string, string?> GetPublicSettings(string? settings);
    IReadOnlyCollection<string> GetConfiguredSecretKeys(string? settings);
    string MergeSettings(string? currentSettings, IReadOnlyDictionary<string, string?> updates);

    Task<MonitoringProviderMonitor> CreateMonitorAsync(Guid organizationId, string name, string url, string? type);
    Task UpdateMonitorAsync(Guid organizationId, string externalId, string? name, string? url, string? type, List<string>? tags);
    Task<List<MonitoringProviderMonitor>> GetMonitorsAsync(Guid organizationId, IReadOnlyCollection<string>? externalIds = null);
    Task<double> GetUptimeAsync(Guid organizationId, string externalId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? monitorName = null);
    Task<(int? Average, int? Lowest, int? Highest)> GetResponseTimeAsync(Guid organizationId, string externalId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? monitorName = null);
    Task DeleteMonitorAsync(Guid organizationId, string externalId);
    Task PauseMonitorAsync(Guid organizationId, string externalId);
    Task StartMonitorAsync(Guid organizationId, string externalId);
    Task<MonitoringProviderAccount> GetAccountDetailsAsync(Guid organizationId);
}

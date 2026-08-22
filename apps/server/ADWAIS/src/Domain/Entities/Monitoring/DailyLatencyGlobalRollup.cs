// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;

namespace Adwais.Domain.Entities.Monitoring;

public class DailyLatencyGlobalRollup
{
    public required DateTimeOffset Date { get; set; }
    public required Guid OrganizationId { get; set; }
    public double? Average { get; set; }
    public double? P10 { get; set; }
    public double? P90 { get; set; }
}



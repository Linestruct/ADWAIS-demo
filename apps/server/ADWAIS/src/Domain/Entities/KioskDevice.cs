// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;

namespace Adwais.Domain.Entities;

public class KioskDevice
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string DeviceId { get; set; }
    public required string ActivationCode { get; set; }
    public DateTimeOffset ActivationCodeExpires { get; set; }
    public bool IsAuthorized { get; set; }
    public DateTimeOffset? AuthorizedAt { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
}
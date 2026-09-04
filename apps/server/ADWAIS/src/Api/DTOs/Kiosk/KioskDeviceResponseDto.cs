// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Api.DTOs.Kiosk;

public record KioskDeviceResponseDto(
    string DeviceId,
    Guid? OrganizationId,
    bool IsAuthorized,
    DateTimeOffset? AuthorizedAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedDate
);

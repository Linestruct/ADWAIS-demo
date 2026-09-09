// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Kiosk;

public record KioskDeviceResponseDto(
    string DeviceId,
    Guid? OrganizationId,
    bool IsAuthorized,
    DateTimeOffset? AuthorizedAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset CreatedDate
);

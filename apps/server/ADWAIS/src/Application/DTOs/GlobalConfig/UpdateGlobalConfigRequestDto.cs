// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.DTOs.GlobalConfig;

public record UpdateGlobalConfigRequestDto(
    int? SystemEventRetentionDays = null);

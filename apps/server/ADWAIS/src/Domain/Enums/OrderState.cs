// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Enums;

public enum OrderState
{
    Unknown,
    Confirmed,
    PendingProcessing,
    Processing,
    Completed,
    Cancelled
}

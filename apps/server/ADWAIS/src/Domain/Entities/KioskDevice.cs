// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Domain.Entities;

public class KioskDevice
{
    public Guid Id { get; set; }
    public required string DeviceId { get; set; }
    public required string ActivationCode { get; set; }
    public DateTimeOffset ActivationCodeExpires { get; set; }
    public bool IsAuthorized { get; set; }
    public DateTimeOffset? AuthorizedAt { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
}
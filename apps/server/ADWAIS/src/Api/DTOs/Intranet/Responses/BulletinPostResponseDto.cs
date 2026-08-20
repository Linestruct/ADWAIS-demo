// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Intranet;

public record BulletinPostResponseDto(
    Guid Id,
    string Title,
    string Body,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    BulletinPostAuthorDto? Author
);

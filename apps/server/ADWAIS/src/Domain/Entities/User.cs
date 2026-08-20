// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;
using Adwais.Domain.Enums;

namespace Adwais.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string? ExternalSubjectId { get; set; }
    public string? Email { get; set; } 
    public required string Name { get; set; }
    public required UserRole Role { get; set; }
    public string? CalendarFeedToken { get; set; }
}



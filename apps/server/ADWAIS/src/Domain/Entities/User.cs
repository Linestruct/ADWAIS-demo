// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.ComponentModel.DataAnnotations;

namespace Adwais.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string? ExternalSubjectId { get; set; }
    public string? Email { get; set; }
    public required string Name { get; set; }
    public string? CalendarFeedToken { get; set; }
}



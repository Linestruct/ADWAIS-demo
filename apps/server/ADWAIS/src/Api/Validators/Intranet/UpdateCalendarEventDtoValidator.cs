// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using FluentValidation;
using Adwais.Application.DTOs.Intranet;

namespace Adwais.Api.Validators.Intranet;

public class UpdateCalendarEventDtoValidator : AbstractValidator<UpdateCalendarEventDto>
{
    public UpdateCalendarEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.EventType)
            .IsInEnum().WithMessage("A valid EventType is required.")
            .When(x => x.EventType != null);

        RuleFor(x => x.Recurrence)
            .IsInEnum().WithMessage("A valid Recurrence is required.")
            .When(x => x.Recurrence != null);

        RuleFor(x => x.EndTime)
            .GreaterThanOrEqualTo(x => x.StartTime!.Value).WithMessage("End time must be greater than or equal to start time.")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);
    }
}

// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using FluentValidation;

namespace Adwais.Api.Validators.GlobalConfig;

public class UpdateGlobalConfigRequestDtoValidator : AbstractValidator<UpdateGlobalConfigRequestDto>
{
    public UpdateGlobalConfigRequestDtoValidator()
    {
        RuleFor(x => x.SystemEventRetentionDays)
            .GreaterThan(0)
            .When(x => x.SystemEventRetentionDays.HasValue)
            .WithMessage("System event retention must be at least 1 day.");
    }
}



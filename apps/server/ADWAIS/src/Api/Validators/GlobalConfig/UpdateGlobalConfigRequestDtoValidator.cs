// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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



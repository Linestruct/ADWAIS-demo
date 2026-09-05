// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Financial;
using FluentValidation;

namespace Adwais.Api.Validators.Financial;

public class TransactionDensityRequestDtoValidator : AbstractValidator<TransactionDensityRequestDto>
{
    public TransactionDensityRequestDtoValidator()
    {
        RuleFor(x => x.Period)
            .IsInEnum().WithMessage("A valid transaction density period is required.");
    }
}

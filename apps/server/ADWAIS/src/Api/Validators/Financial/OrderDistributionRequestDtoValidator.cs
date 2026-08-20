// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Financial;
using FluentValidation;

namespace Adwais.Api.Validators.Financial;

public class OrderDistributionRequestDtoValidator : AbstractValidator<OrderDistributionRequestDto>
{
    public OrderDistributionRequestDtoValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required.");

        RuleFor(x => x.BinCount)
            .GreaterThan(0)
            .When(x => x.BinCount.HasValue)
            .WithMessage("BinCount must be greater than 0.");
    }
}



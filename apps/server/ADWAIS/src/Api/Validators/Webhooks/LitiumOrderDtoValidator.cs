// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using FluentValidation;
using Adwais.Application.DTOs.Financial.Upstream;

namespace Adwais.Api.Validators.Webhooks;

public class LitiumOrderDtoValidator : AbstractValidator<LitiumSyncResponse.LitiumOrderDto>
{
    public LitiumOrderDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("OrderNumber is required.")
            .When(x => !string.Equals(x.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.CreatedDate)
            .NotEmpty().WithMessage("CreatedDate is required and must be valid.");

        RuleFor(x => x.OrderStatus)
            .NotEmpty().WithMessage("OrderStatus is required.");

        RuleFor(x => x.TotalValueIncludingVat)
            .NotNull().WithMessage("TotalValueIncludingVat is required.");

        RuleFor(x => x.TotalValueExcludingVat)
            .NotNull().WithMessage("TotalValueExcludingVat is required.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .When(x => !string.Equals(x.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase));
    }
}

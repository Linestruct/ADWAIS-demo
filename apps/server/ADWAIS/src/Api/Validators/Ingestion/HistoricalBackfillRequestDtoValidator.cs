// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Ingestion;
using FluentValidation;

namespace Adwais.Api.Validators.Ingestion;

public class HistoricalBackfillRequestDtoValidator : AbstractValidator<HistoricalBackfillRequestDto>
{
    public HistoricalBackfillRequestDtoValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        
        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("StartDate must be before EndDate.");

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .WithMessage("EndDate cannot be in the future.");
    }
}


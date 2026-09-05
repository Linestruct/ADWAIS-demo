// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Monitoring;
using FluentValidation;

namespace Adwais.Api.Validators.Monitoring;

public class UpdateMonitorRequestDtoValidator : AbstractValidator<UpdateMonitorRequestDto>
{
    public UpdateMonitorRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().When(x => x.Name is not null)
            .MaximumLength(100);

        RuleFor(x => x.Url)
            .NotEmpty().When(x => x.Url is not null)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => x.Url is not null)
            .WithMessage("A valid absolute URL is required.");

        RuleFor(x => x.Type)
            .NotEmpty().When(x => x.Type is not null)
            .MaximumLength(50)
            .WithMessage("Monitor type must not exceed 50 characters.");

        RuleFor(x => x.Sla)
            .InclusiveBetween(0, 100)
            .When(x => x.Sla.HasValue)
            .WithMessage("Uptime SLA must be between 0 and 100.");

        RuleFor(x => x.LatencyDegradedFloor)
            .GreaterThan(0)
            .When(x => x.LatencyDegradedFloor.HasValue)
            .WithMessage("Latency degraded floor must be greater than 0 ms.");
    }
}



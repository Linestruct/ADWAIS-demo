// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Monitoring;
using FluentValidation;

namespace Adwais.Api.Validators.Monitoring;

public class CreateMonitorRequestDtoValidator : AbstractValidator<CreateMonitorRequestDto>
{
    public CreateMonitorRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Monitor name is required.")
            .MaximumLength(100).WithMessage("Monitor name must not exceed 100 characters.");

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("A valid absolute URL is required.");

        RuleFor(x => x.Type)
            .NotEmpty().When(x => x.Type is not null)
            .MaximumLength(50)
            .WithMessage("Monitor type must not exceed 50 characters.");

        RuleFor(x => x.UptimeSla)
            .InclusiveBetween(0, 100).When(x => x.UptimeSla.HasValue)
            .WithMessage("Uptime SLA must be between 0 and 100.");

        RuleFor(x => x.LatencyDegradedFloor)
            .GreaterThan(0)
            .When(x => x.LatencyDegradedFloor.HasValue)
            .WithMessage("Latency degraded floor must be greater than 0 ms.");
    }
}



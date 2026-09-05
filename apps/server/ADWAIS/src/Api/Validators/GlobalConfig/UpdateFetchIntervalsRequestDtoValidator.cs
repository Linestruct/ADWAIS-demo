// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.GlobalConfig;
using FluentValidation;

namespace Adwais.Api.Validators.GlobalConfig;

public class UpdateFetchIntervalsRequestDtoValidator : AbstractValidator<UpdateFetchIntervalsRequestDto>
{
    public UpdateFetchIntervalsRequestDtoValidator()
    {
        RuleFor(x => x.OrderFetchIntervalMinutes)
            .GreaterThan(0)
            .When(x => x.OrderFetchIntervalMinutes.HasValue)
            .WithMessage("Order fetch interval must be at least 1 minute.");

        RuleFor(x => x.UptimeFetchIntervalMinutes)
            .GreaterThan(0)
            .When(x => x.UptimeFetchIntervalMinutes.HasValue)
            .WithMessage("Uptime fetch interval must be at least 1 minute.");

        RuleFor(x => x.UserStatsFetchIntervalMinutes)
            .GreaterThan(0)
            .When(x => x.UserStatsFetchIntervalMinutes.HasValue)
            .WithMessage("User stats fetch interval must be at least 1 minute.");

        RuleFor(x => x.LatencyFetchIntervalMinutes)
            .GreaterThan(0)
            .When(x => x.LatencyFetchIntervalMinutes.HasValue)
            .WithMessage("Latency fetch interval must be at least 1 minute.");

        RuleFor(x => x.FeedFetchIntervalHours)
            .GreaterThan(0)
            .When(x => x.FeedFetchIntervalHours.HasValue)
            .WithMessage("Feed fetch interval must be at least 1 hour.");
    }
}

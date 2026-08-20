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
    public UpdateGlobalConfigRequestDtoValidator(IEnumerable<IMonitoringProvider> monitoringProviders)
    {
        RuleFor(x => x.SystemEventRetentionDays)
            .GreaterThan(0)
            .When(x => x.SystemEventRetentionDays.HasValue)
            .WithMessage("System event retention must be at least 1 day.");

        RuleFor(x => x.FeedFetchIntervalHours)
            .GreaterThan(0)
            .When(x => x.FeedFetchIntervalHours.HasValue)
            .WithMessage("Feed fetch interval must be at least 1 hour.");

        RuleFor(x => x.MonitoringProvider)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .Must(provider => monitoringProviders.Any(candidate => candidate.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Monitoring provider is not registered.")
            .When(x => x.MonitoringProvider is not null);

        RuleFor(x => x.ReportingTimeZoneId)
            .NotEmpty()
            .MaximumLength(100)
            .Must(BeValidTimeZoneId)
            .When(x => x.ReportingTimeZoneId is not null)
            .WithMessage("Reporting timezone must be a valid IANA timezone identifier.");
    }

    private static bool BeValidTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return false;
        var trimmedTimeZoneId = timeZoneId.Trim();
        if (!string.Equals(trimmedTimeZoneId, "UTC", StringComparison.Ordinal)
            && !trimmedTimeZoneId.Contains('/'))
            return false;

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(trimmedTimeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}



// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using FluentValidation;
using Adwais.Application.DTOs.Intranet;

namespace Adwais.Api.Validators.Intranet;

public class UpdateCalendarSubscriptionDtoValidator : AbstractValidator<UpdateCalendarSubscriptionDto>
{
    public UpdateCalendarSubscriptionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL cannot be empty.")
            .MaximumLength(2048).WithMessage("URL must not exceed 2048 characters.")
            .Must(LinkMustBeAValidUri).WithMessage("URL must be a valid absolute HTTP or HTTPS address.")
            .When(x => x.Url != null);
    }

    private bool LinkMustBeAValidUri(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

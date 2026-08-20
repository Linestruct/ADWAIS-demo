// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using FluentValidation;
using Adwais.Api.DTOs.Tenants;
using Adwais.Application.Interfaces;

namespace Adwais.Api.Validators.Tenants;

public class UpdateTenantRequestDtoValidator :  AbstractValidator<UpdateTenantRequestDto>
{
    public UpdateTenantRequestDtoValidator(IEnumerable<IOrderSource> orderSources)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name cannot be empty.")
            .When(x => x.Name is not null);
        RuleFor(x => x.OrderProvider)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .Must(provider => orderSources.Any(source => source.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Order provider is not registered.")
            .When(x => x.OrderProvider is not null);

        RuleFor(x => x.ImageUrl)
            .Must(uri => string.IsNullOrWhiteSpace(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("INVALID IMAGE URL.");
    }

}


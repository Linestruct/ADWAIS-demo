// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using FluentValidation;
using Adwais.Api.DTOs.Tenants;
using Adwais.Application.Interfaces;

namespace Adwais.Api.Validators.Tenants;

public class CreateTenantRequestDtoValidator : AbstractValidator<CreateTenantRequestDto>
{
    public CreateTenantRequestDtoValidator(IEnumerable<IOrderSource> orderSources)
    {
        RuleFor(x => x.Name).NotEmpty().
            WithMessage("Name is required.");
        RuleFor(x => x.OrderProvider)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .Must(provider => orderSources.Any(source => source.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Order provider is not registered.");
        
        RuleFor(x => x.OrderProviderSettings).NotNull().
            WithMessage("Order provider settings are required when order fetching is enabled.")
            .When(x => x.OrderFetchingEnabled);
        RuleFor(x => x.ImageUrl).Must(uri =>
            string.IsNullOrWhiteSpace(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("INVALID IMAGE URL");
    }

}


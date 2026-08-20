// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Users;
using FluentValidation;

namespace Adwais.Api.Validators.Users;

public class UpdateUserRequestDtoValidator : AbstractValidator<UpdateUserRequestDto>
{
    public UpdateUserRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().When(x => x.Name != null)
            .WithMessage("User name cannot be empty.")
            .MaximumLength(100).When(x => x.Name != null)
            .WithMessage("User name must not exceed 100 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().When(x => x.Role.HasValue)
            .WithMessage("A valid user role is required.");
    }
}



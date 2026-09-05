// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Users;
using FluentValidation;

namespace Adwais.Api.Validators.Users;

public class CreateUserRequestDtoValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("User email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(255).WithMessage("User email must not exceed 255 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("A valid user role is required.");
    }
}



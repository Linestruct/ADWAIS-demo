// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Kiosk;
using FluentValidation;

namespace Adwais.Api.Validators.Kiosk;

public class RegisterKioskRequestDtoValidator : AbstractValidator<RegisterKioskRequestDto>
{
    public RegisterKioskRequestDtoValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty().WithMessage("Device identifier is required.")
            .MaximumLength(255).WithMessage("Device identifier must not exceed 255 characters.");
    }
}

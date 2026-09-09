// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Organizations;
using FluentValidation;

namespace Adwais.Api.Validators.Organizations;

public class CreateOrganizationRequestDtoValidator : AbstractValidator<CreateOrganizationRequestDto>
{
    public CreateOrganizationRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(255).WithMessage("Organization name must not exceed 255 characters.");
    }
}

public class UpdateOrganizationRequestDtoValidator : AbstractValidator<UpdateOrganizationRequestDto>
{
    public UpdateOrganizationRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required.")
            .MaximumLength(255).WithMessage("Organization name must not exceed 255 characters.");
    }
}

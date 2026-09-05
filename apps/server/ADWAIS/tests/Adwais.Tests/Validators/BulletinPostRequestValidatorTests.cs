// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using FluentValidation.TestHelper;
using Adwais.Application.DTOs.Intranet;
using Adwais.Api.Validators.Intranet;
using Xunit;

namespace Adwais.Tests.Validators;

public class BulletinPostRequestValidatorTests
{
    private readonly CreateBulletinPostDtoValidator _validator = new();

    [Fact]
    public void Validator_ShouldHaveError_WhenTitleIsEmpty()
    {
        var model = new CreateBulletinPostDto { Title = "", Body = "Body" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenTitleExceedsMaxLength()
    {
        var model = new CreateBulletinPostDto { Title = new string('a', 256), Body = "Body" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenBodyIsEmpty()
    {
        var model = new CreateBulletinPostDto { Title = "Title", Body = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenBodyExceedsMaxLength()
    {
        var model = new CreateBulletinPostDto { Title = "Title", Body = new string('a', 5001) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Body);
    }

    [Fact]
    public void Validator_ShouldNotHaveError_WhenModelIsValid()
    {
        var model = new CreateBulletinPostDto { Title = "Valid Title", Body = "Valid Body" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

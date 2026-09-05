// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using FluentValidation.TestHelper;
using Adwais.Application.DTOs.Intranet;
using Adwais.Api.Validators.Intranet;
using Xunit;

namespace Adwais.Tests.Validators;

public class GetFeedsRequestValidatorTests
{
    private readonly GetFeedsRequestValidator _validator = new();

    [Fact]
    public void Validator_ShouldHaveError_WhenPageIsLessThanOne()
    {
        var model = new GetFeedsRequest { Page = 0 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenPageSizeIsLessThanOne()
    {
        var model = new GetFeedsRequest { PageSize = 0 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenPageSizeIsGreaterThanOneHundred()
    {
        var model = new GetFeedsRequest { PageSize = 101 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validator_ShouldNotHaveError_WhenModelIsValid()
    {
        var model = new GetFeedsRequest { Page = 1, PageSize = 25, AuthorName = "litium" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenAuthorNameExceedsMaxLength()
    {
        var model = new GetFeedsRequest { AuthorName = new string('a', 256) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AuthorName);
    }
}

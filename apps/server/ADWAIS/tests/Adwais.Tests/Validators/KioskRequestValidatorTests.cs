// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using FluentValidation.TestHelper;
using Adwais.Api.DTOs.Kiosk;
using Adwais.Api.Validators.Kiosk;
using Xunit;

namespace Adwais.Tests.Validators;

public class KioskRequestValidatorTests
{
    private readonly RegisterKioskRequestDtoValidator _registerValidator;
    private readonly ActivateKioskRequestDtoValidator _activateValidator;

    public KioskRequestValidatorTests()
    {
        _registerValidator = new RegisterKioskRequestDtoValidator();
        _activateValidator = new ActivateKioskRequestDtoValidator();
    }

    [Fact]
    public void RegisterValidator_ShouldHaveError_WhenDeviceIdIsEmpty()
    {
        var model = new RegisterKioskRequestDto { DeviceId = "" };
        var result = _registerValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId);
    }

    [Fact]
    public void RegisterValidator_ShouldHaveError_WhenDeviceIdExceedsMaxLength()
    {
        var model = new RegisterKioskRequestDto { DeviceId = new string('a', 256) };
        var result = _registerValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DeviceId);
    }

    [Fact]
    public void RegisterValidator_ShouldNotHaveError_WhenDeviceIdIsValid()
    {
        var model = new RegisterKioskRequestDto { DeviceId = "kiosk-device-123" };
        var result = _registerValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.DeviceId);
    }

    [Fact]
    public void ActivateValidator_ShouldHaveError_WhenActivationCodeIsEmpty()
    {
        var model = new ActivateKioskRequestDto { ActivationCode = "" };
        var result = _activateValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ActivationCode);
    }

    [Fact]
    public void ActivateValidator_ShouldHaveError_WhenActivationCodeIsTooShort()
    {
        var model = new ActivateKioskRequestDto { ActivationCode = "12345" };
        var result = _activateValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ActivationCode);
    }

    [Fact]
    public void ActivateValidator_ShouldHaveError_WhenActivationCodeIsTooLong()
    {
        var model = new ActivateKioskRequestDto { ActivationCode = "1234567" };
        var result = _activateValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ActivationCode);
    }

    [Fact]
    public void ActivateValidator_ShouldNotHaveError_WhenActivationCodeIsValid()
    {
        var model = new ActivateKioskRequestDto { ActivationCode = "AB39XZ" };
        var result = _activateValidator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.ActivationCode);
    }
}

// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using FluentValidation.TestHelper;
using Adwais.Api.DTOs.Monitoring;
using Adwais.Api.Validators.Monitoring;
using Xunit;

namespace Adwais.Tests.Validators;

public class MonitorValidatorsTests
{
    private readonly CreateMonitorRequestDtoValidator _createValidator = new();
    private readonly UpdateMonitorRequestDtoValidator _updateValidator = new();
    private readonly UptimeMonitorDtoValidator _dtoValidator = new();

    [Fact]
    public void CreateMonitorRequestDtoValidator_ShouldFail_WhenNameIsEmptyOrTooLong()
    {
        var modelEmpty = CreateMonitorRequest(name: string.Empty);
        var resultEmpty = _createValidator.TestValidate(modelEmpty);
        resultEmpty.ShouldHaveValidationErrorFor(x => x.Name);

        var modelLong = CreateMonitorRequest(name: new string('a', 101));
        var resultLong = _createValidator.TestValidate(modelLong);
        resultLong.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateMonitorRequestDtoValidator_ShouldFail_WhenUrlIsInvalid()
    {
        var model = CreateMonitorRequest(url: "invalid-url");
        var result = _createValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public void CreateMonitorRequestDtoValidator_ShouldFail_WhenSlaIsOutOfRange()
    {
        var model = CreateMonitorRequest(uptimeSla: -10.0);
        var result = _createValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.UptimeSla);
    }

    [Fact]
    public void CreateMonitorRequestDtoValidator_ShouldPass_WhenModelIsValid()
    {
        var model = CreateMonitorRequest(type: null, uptimeSla: 99.5);
        var result = _createValidator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MonitorRequestValidators_ShouldRejectBlankType_WhenProvided()
    {
        _createValidator.TestValidate(CreateMonitorRequest(type: ""))
            .ShouldHaveValidationErrorFor(x => x.Type);
        _updateValidator.TestValidate(new UpdateMonitorRequestDto { Type = "" })
            .ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void UpdateMonitorRequestDtoValidator_ShouldFail_WhenSlaIsOutOfRange()
    {
        var model = new UpdateMonitorRequestDto { Sla = -5.0 };
        var result = _updateValidator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Sla);
    }

    [Fact]
    public void UpdateMonitorRequestDtoValidator_ShouldPass_WhenSlaIsValidOrOmitted()
    {
        var modelValid = new UpdateMonitorRequestDto { Sla = 99.0 };
        var resultValid = _updateValidator.TestValidate(modelValid);
        resultValid.ShouldNotHaveAnyValidationErrors();

        var modelOmitted = new UpdateMonitorRequestDto();
        var resultOmitted = _updateValidator.TestValidate(modelOmitted);
        resultOmitted.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UptimeMonitorDtoValidator_ShouldFail_WhenFieldsAreEmptyOrInvalid()
    {
        var modelEmpty = CreateUptimeMonitorDto(1, string.Empty, "https://url.com");
        var resultEmpty = _dtoValidator.TestValidate(modelEmpty);
        resultEmpty.ShouldHaveValidationErrorFor(x => x.Name);

        var modelInvalidUrl = CreateUptimeMonitorDto(1, "Monitor", "invalid-url");
        var resultInvalidUrl = _dtoValidator.TestValidate(modelInvalidUrl);
        resultInvalidUrl.ShouldHaveValidationErrorFor(x => x.Url);
    }

    private static UptimeMonitorDto CreateUptimeMonitorDto(int id, string name, string url)
    {
        return new UptimeMonitorDto(
            Id: id,
            TenantId: Guid.NewGuid(),
            TenantName: "Tenant",
            Type: "HTTP",
            Name: name,
            Url: url,
            UpdateInterval: 300,
            LatencyDegradedFloor: null,
            UptimeSla: null,
            CurrentUptimePercentage: null,
            CurrentLatency: null,
            UptimeMonitorEnabled: true,
            CurrentStatus: "up",
            LastUpdate: null,
            LastUptimeUpdate: null,
            LastLatencyUpdate: null,
            CreatedDate: DateTimeOffset.UtcNow,
            LastSyncError: null,
            Tags: new List<string>(),
            TenantBaseUrl: null,
            TenantImageUrl: null,
            HttpMethod: null,
            TimeoutSeconds: null,
            SslExpiresAt: null,
            DomainExpiresAt: null,
            MonitoredRegions: new List<string>(),
            CurrentStateDurationSeconds: null,
            LatestIncident: null,
            Provider: "uptimerobot",
            ExternalId: id.ToString()
        );
    }

    private static CreateMonitorRequestDto CreateMonitorRequest(
        string name = "Monitor",
        string url = "https://url.com",
        string? type = "HTTP",
        double? uptimeSla = null) => new()
    {
        Name = name,
        Url = url,
        Type = type,
        UptimeSla = uptimeSla
    };
}

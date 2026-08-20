// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Adwais.Application.Interfaces;
using Adwais.Application.Common.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Helpers;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Service implementation managing kiosk registration, activation, and token retrieval flows.
/// </summary>
public class KioskService(
    IApplicationDbContext dbContext,
    ITokenService tokenService)
    : IKioskService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ITokenService _tokenService = tokenService;

    /// <inheritdoc />
    public async Task<string> RegisterDeviceAsync(string deviceId, CancellationToken ct = default)
    {
        string code;
        bool isDuplicate;
        do
        {
            code = AuthStringHelper.GetRandomActivationCode();
            isDuplicate = await _dbContext.KioskDevices.AnyAsync(kd => 
                kd.ActivationCode == code && 
                !kd.IsAuthorized && 
                kd.ActivationCodeExpires > DateTimeOffset.UtcNow, ct);
        } while (isDuplicate);
        
        var device = await _dbContext.KioskDevices.SingleOrDefaultAsync(kd => kd.DeviceId == deviceId, ct);
        var expiry = DateTimeOffset.UtcNow.AddMinutes(10);
        if (device == null)
        {
            _dbContext.KioskDevices.Add(new KioskDevice
            {
                DeviceId = deviceId,
                ActivationCode = code,
                ActivationCodeExpires = expiry,
                IsAuthorized = false,
                AuthorizedAt = null,
                CreatedDate = DateTimeOffset.UtcNow
            });
        }
        else
        {
            device.ActivationCode = code;
            device.ActivationCodeExpires = expiry;
            device.IsAuthorized = false;
            device.AuthorizedAt = null;
        }
        await _dbContext.SaveChangesAsync(ct);
        return code;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateDeviceAsync(string activationCode, CancellationToken ct = default)
    {
        var device = await _dbContext.KioskDevices.SingleOrDefaultAsync(kd => 
            kd.ActivationCode == activationCode && 
            !kd.IsAuthorized && 
            kd.ActivationCodeExpires > DateTimeOffset.UtcNow, ct);

        if (device == null)
        {
            return false;
        }
        
        device.IsAuthorized = true;
        device.AuthorizedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(string deviceId, CancellationToken ct = default)
    {
        var device = await _dbContext.KioskDevices.SingleOrDefaultAsync(kd => kd.DeviceId == deviceId, ct);

        if (device is null || !device.IsAuthorized)
        {
            return null;
        }

        return _tokenService.GenerateKioskToken(deviceId);
    }
}

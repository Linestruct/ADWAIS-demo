// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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

        // Fingerprints change: a new local id orphans the old row. Remove
        // rows that never authorized and whose code expired over a week ago.
        // Authorized rows stay: they let a returning display skip staff.
        var staleCutoff = DateTimeOffset.UtcNow.AddDays(-7);
        var stale = await _dbContext.KioskDevices
            .Where(kd => !kd.IsAuthorized && kd.ActivationCodeExpires < staleCutoff)
            .ToListAsync(ct);
        _dbContext.KioskDevices.RemoveRange(stale);

        await _dbContext.SaveChangesAsync(ct);
        return code;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateDeviceAsync(string activationCode, Guid organizationId, CancellationToken ct = default)
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
        device.OrganizationId = organizationId;

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

        device.LastSeenAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return _tokenService.GenerateKioskToken(deviceId, organizationId: device.OrganizationId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KioskDevice>> GetDevicesAsync(Guid? organizationId, CancellationToken ct = default)
    {
        var query = _dbContext.KioskDevices.AsNoTracking();
        if (organizationId.HasValue)
            query = query.Where(kd => kd.OrganizationId == organizationId.Value);

        return await query
            .OrderByDescending(kd => kd.CreatedDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDeviceAsync(string deviceId, Guid? organizationId, CancellationToken ct = default)
    {
        var device = await _dbContext.KioskDevices.SingleOrDefaultAsync(kd => kd.DeviceId == deviceId, ct);
        if (device is null)
            return false;
        if (organizationId.HasValue && device.OrganizationId != organizationId.Value)
            return false;

        _dbContext.KioskDevices.Remove(device);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}

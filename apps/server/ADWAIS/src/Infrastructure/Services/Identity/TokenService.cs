// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Adwais.Application.Interfaces;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Service implementation for generating locally signed JWT tokens for kiosk display devices.
/// </summary>
public class TokenService(IConfiguration configuration) : ITokenService
{
    private readonly IConfiguration _configuration = configuration;

    /// <inheritdoc />
    public string GenerateKioskToken(string deviceId, string role = "Viewer")
    {
        var secret = _configuration["Authentication:KioskJwtSecret"];
        if (string.IsNullOrEmpty(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("Kiosk JWT secret must be configured and at least 32 characters long.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        tokenHandler.OutboundClaimTypeMap.Clear();
        var key = Encoding.UTF8.GetBytes(secret);

            var issuer = _configuration["Authentication:KioskJwtIssuer"] ?? "ADWAIS";
            var audience = _configuration["Authentication:KioskJwtAudience"] ?? "ADWAIS-Kiosk";

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", deviceId),
                    new Claim(ClaimTypes.NameIdentifier, deviceId),
                    new Claim("name", $"Kiosk-Device-{deviceId}"),
                    new Claim("role", role)
                }),
                Expires = DateTime.UtcNow.AddDays(30),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

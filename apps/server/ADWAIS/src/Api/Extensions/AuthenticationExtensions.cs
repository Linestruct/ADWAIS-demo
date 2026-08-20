// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Adwais.Infrastructure.Security;

namespace Adwais.Api.Extensions;

public static class AuthenticationExtensions
{
    public const string DashboardCookieScheme = "DashboardCookie";
    public const string DashboardCookieName = "adwais_dashboard";
    private const string AppAuthenticationScheme = "AppAuthentication";

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Authentication Schemes
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = AppAuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });
        var kioskIssuer = configuration["Authentication:KioskJwtIssuer"] ?? "ADWAIS";
        var kioskAudience = configuration["Authentication:KioskJwtAudience"] ?? "ADWAIS-Kiosk";

        authBuilder.AddPolicyScheme(AppAuthenticationScheme, AppAuthenticationScheme, options =>
        {
            options.ForwardDefaultSelector = context =>
                context.Request.Path.StartsWithSegments("/hangfire")
                && context.Request.Cookies.ContainsKey(DashboardCookieName)
                    ? DashboardCookieScheme
                    : JwtBearerDefaults.AuthenticationScheme;
        });

        authBuilder.AddJwtBearer(options =>
        {
            options.Authority = configuration["Authentication:OidcAuthority"];
            options.Audience = configuration["Authentication:OidcAudience"];
            options.RequireHttpsMetadata = !string.Equals(
                configuration["ASPNETCORE_ENVIRONMENT"],
                "Development",
                StringComparison.OrdinalIgnoreCase);
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "role",
                ValidateIssuer = true,
                ValidateAudience = true
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authHeader.Substring(7).Trim();
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token) && handler.ReadJwtToken(token).Issuer == kioskIssuer)
                        {
                            context.NoResult();
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        authBuilder.AddJwtBearer("KioskJwt", options =>
        {
            options.MapInboundClaims = false;
            var secret = configuration["Authentication:KioskJwtSecret"];
            if (string.IsNullOrEmpty(secret))
            {
                // Generate a secure transient key on startup to satisfy EF/OpenAPI tools 
                // while effectively disabling JWT token validation for external clients
                secret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            }
            var key = Encoding.UTF8.GetBytes(secret);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = kioskIssuer,
                ValidateAudience = true,
                ValidAudience = kioskAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authHeader.Substring(7).Trim();
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token))
                        {
                            var jwtToken = handler.ReadJwtToken(token);
                            if (jwtToken.Issuer != kioskIssuer)
                            {
                                // Skip this scheme for non-Kiosk tokens to prevent validation error spam
                                context.NoResult();
                            }
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });
        authBuilder.AddCookie(DashboardCookieScheme, options =>
        {
            options.Cookie.Name = DashboardCookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/hangfire";
            options.Cookie.SameSite = SameSiteMode.Strict;
            var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
                ?? configuration["DOTNET_ENVIRONMENT"];
            var isDevelopment = string.Equals(
                environmentName,
                "Development",
                StringComparison.OrdinalIgnoreCase);
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.SlidingExpiration = false;
        });

        // Register claims transformation for external OIDC users.
        services.AddTransient<IClaimsTransformation, LocalUserClaimsTransformation>();

        // Configure Authorization Policies
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
            {
                policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                policy.AuthenticationSchemes.Add("KioskJwt");
                policy.RequireRole("Admin");
            });

            options.AddPolicy("StaffAccess", policy =>
            {
                policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                policy.AuthenticationSchemes.Add("KioskJwt");
                policy.RequireRole("Admin", "Employee");
            });

            options.AddPolicy("KioskOrStaffAccess", policy =>
            {
                policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                policy.AuthenticationSchemes.Add("KioskJwt");
                policy.RequireRole("Admin", "Employee", "Viewer");
            });
        });

        return services;
    }
}

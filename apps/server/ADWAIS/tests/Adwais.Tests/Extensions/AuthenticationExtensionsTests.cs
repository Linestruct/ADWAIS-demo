// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace Adwais.Tests.Extensions;

public class AuthenticationExtensionsTests
{
    [Fact]
    public async Task RegistersExpectedSchemesAndRoutesHangfireCookiesToDashboardScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAppAuthentication(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:OidcAuthority"] = "https://issuer.example.com",
                ["Authentication:OidcAudience"] = "adwais-api",
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["Authentication:KioskJwtSecret"] = "SuperSecretKeyForTestingKioskTokens32CharsMinimum!",
                ["Authentication:KioskJwtIssuer"] = "ADWAIS",
                ["Authentication:KioskJwtAudience"] = "ADWAIS-Kiosk"
            })
            .Build());

        await using var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await schemeProvider.GetAllSchemesAsync();

        Assert.Contains(schemes, scheme => scheme.Name == "AppAuthentication");
        Assert.Contains(schemes, scheme => scheme.Name == JwtBearerDefaults.AuthenticationScheme);
        Assert.Contains(schemes, scheme => scheme.Name == "KioskJwt");
        Assert.Contains(schemes, scheme => scheme.Name == AuthenticationExtensions.DashboardCookieScheme);

        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.Equal("https://issuer.example.com", jwtOptions.Authority);
        Assert.Equal("adwais-api", jwtOptions.Audience);
        Assert.False(jwtOptions.RequireHttpsMetadata);
        Assert.Equal("name", jwtOptions.TokenValidationParameters.NameClaimType);
        Assert.Equal("role", jwtOptions.TokenValidationParameters.RoleClaimType);

        var policyOptions = provider.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get("AppAuthentication");
        var apiContext = new DefaultHttpContext();
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, policyOptions.ForwardDefaultSelector!(apiContext));

        var dashboardContext = new DefaultHttpContext();
        dashboardContext.Request.Path = "/hangfire";
        dashboardContext.Request.Headers.Cookie = $"{AuthenticationExtensions.DashboardCookieName}=session";
        Assert.Equal(AuthenticationExtensions.DashboardCookieScheme, policyOptions.ForwardDefaultSelector(dashboardContext));

        var cookieOptions = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(AuthenticationExtensions.DashboardCookieScheme);
        Assert.Equal(AuthenticationExtensions.DashboardCookieName, cookieOptions.Cookie.Name);
        Assert.Equal("/hangfire", cookieOptions.Cookie.Path);
        Assert.True(cookieOptions.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, cookieOptions.Cookie.SecurePolicy);
        Assert.Equal(TimeSpan.FromMinutes(5), cookieOptions.ExpireTimeSpan);

        var authorization = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var adminPolicy = await authorization.GetPolicyAsync("AdminOnly");
        Assert.NotNull(adminPolicy);
        Assert.Contains(JwtBearerDefaults.AuthenticationScheme, adminPolicy.AuthenticationSchemes);
        Assert.Contains("KioskJwt", adminPolicy.AuthenticationSchemes);
        Assert.Contains(adminPolicy.Requirements, requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement roles && roles.AllowedRoles.Contains("Admin"));

        var staffPolicy = await authorization.GetPolicyAsync("StaffAccess");
        Assert.NotNull(staffPolicy);
        Assert.Contains(JwtBearerDefaults.AuthenticationScheme, staffPolicy.AuthenticationSchemes);
        Assert.Contains("KioskJwt", staffPolicy.AuthenticationSchemes);
        var staffRoles = Assert.Single(staffPolicy.Requirements.OfType<Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement>());
        Assert.Equal(new[] { "Admin", "Employee" }, staffRoles.AllowedRoles.Order().ToArray());

        var kioskToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: "ADWAIS"));
        var kioskMessage = new MessageReceivedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            jwtOptions);
        kioskMessage.HttpContext.Request.Headers.Authorization = $"Bearer {kioskToken}";
        await jwtOptions.Events.MessageReceived(kioskMessage);
        Assert.NotNull(kioskMessage.Result);

        var externalToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: "https://issuer.example.com"));
        var externalMessage = new MessageReceivedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            jwtOptions);
        externalMessage.HttpContext.Request.Headers.Authorization = $"Bearer {externalToken}";
        await jwtOptions.Events.MessageReceived(externalMessage);
        Assert.Null(externalMessage.Result);

        var kioskOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get("KioskJwt");
        var kioskExternalMessage = new MessageReceivedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme("KioskJwt", null, typeof(JwtBearerHandler)),
            kioskOptions);
        kioskExternalMessage.HttpContext.Request.Headers.Authorization = $"Bearer {externalToken}";
        await kioskOptions.Events.MessageReceived(kioskExternalMessage);
        Assert.NotNull(kioskExternalMessage.Result);
    }

    [Fact]
    public void UsesAlwaysSecureDashboardCookieOutsideDevelopment()
    {
        var services = new ServiceCollection();
        services.AddAppAuthentication(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build());

        using var provider = services.BuildServiceProvider();
        var cookieOptions = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(AuthenticationExtensions.DashboardCookieScheme);

        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);
    }
}

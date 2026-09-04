// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using Adwais.Infrastructure.Services;

namespace Adwais.Tests.Services;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _configMock;

    public TokenServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Authentication:KioskJwtSecret"]).Returns(_secret);
    }

    private readonly string _secret = "SuperSecretKeyForTestingKioskTokens32CharsMinimum!";

    [Fact]
    public void GenerateKioskToken_ShouldReturnValidJwtWithCorrectClaims()
    {
        // Arrange
        var tokenService = new TokenService(_configMock.Object);
        var deviceId = "kiosk-test-device";

        // Act
        var tokenString = tokenService.GenerateKioskToken(deviceId);

        // Assert
        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        // Decode JWT and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        Assert.Equal("ADWAIS", jwtToken.Issuer);
        Assert.Equal("ADWAIS-Kiosk", jwtToken.Audiences.First());
        
        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value;
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;

        Assert.Equal($"Kiosk-Device-{deviceId}", nameClaim);
        Assert.Equal("Viewer", roleClaim);

        // Verify expiration is around one hour from now
        var diff = jwtToken.ValidTo - DateTime.UtcNow;
        Assert.True(diff.TotalHours >= 0.9 && diff.TotalHours <= 1.1);
    }

    [Fact]
    public void GenerateKioskToken_WithOrganization_EmbedsOrgClaim()
    {
        var tokenService = new TokenService(_configMock.Object);
        var orgId = Guid.NewGuid();

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(
            tokenService.GenerateKioskToken("kiosk-org-device", organizationId: orgId));

        Assert.Equal(orgId.ToString(), jwtToken.Claims.FirstOrDefault(c => c.Type == "org_id")?.Value);
    }

    [Fact]
    public void GenerateKioskToken_WithoutOrganization_HasNoOrgClaim()
    {
        var tokenService = new TokenService(_configMock.Object);

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(
            tokenService.GenerateKioskToken("kiosk-plain-device"));

        Assert.Null(jwtToken.Claims.FirstOrDefault(c => c.Type == "org_id"));
    }

    [Fact]
    public void GenerateKioskToken_WithPlatformFlag_EmbedsPlatformClaim()
    {
        var tokenService = new TokenService(_configMock.Object);

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(
            tokenService.GenerateKioskToken("dev-admin", "Admin", isPlatformAdmin: true));

        Assert.Equal("true", jwtToken.Claims.FirstOrDefault(c => c.Type == "is_platform_admin")?.Value);
        Assert.Null(jwtToken.Claims.FirstOrDefault(c => c.Type == "org_id"));
    }

    [Fact]
    public void GenerateKioskToken_WithoutPlatformFlag_HasNoPlatformClaim()
    {
        var tokenService = new TokenService(_configMock.Object);

        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(
            tokenService.GenerateKioskToken("kiosk-plain-device"));

        Assert.Null(jwtToken.Claims.FirstOrDefault(c => c.Type == "is_platform_admin"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateKioskToken_ShouldThrowException_WhenSecretIsNullOrEmpty(string? invalidSecret)
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Authentication:KioskJwtSecret"]).Returns(invalidSecret);
        var tokenService = new TokenService(configMock.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenService.GenerateKioskToken("kiosk-test-device"));
    }

    [Fact]
    public void GenerateKioskToken_ShouldThrowException_WhenSecretIsTooShort()
    {
        // Arrange
        var shortSecret = "too-short";
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Authentication:KioskJwtSecret"]).Returns(shortSecret);
        var tokenService = new TokenService(configMock.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => tokenService.GenerateKioskToken("kiosk-test-device"));
    }
}

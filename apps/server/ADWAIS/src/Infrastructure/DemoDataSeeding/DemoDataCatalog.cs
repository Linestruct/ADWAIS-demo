// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;

namespace Adwais.Infrastructure.DemoDataSeeding;

internal sealed record DemoTenantProfile(
    string Name,
    TenantType Type,
    string BaseUrl,
    int MinAov,
    int MaxAov,
    int DailyVolume,
    int VolumeVariance,
    decimal SeasonalMultiplier,
    DemoOrderValueProfile OrderValues);

internal sealed record DemoOrderValueProfile(
    decimal MedianAov,
    double LogStandardDeviation,
    decimal? SecondaryMedianAov = null,
    double SecondaryWeight = 0);

internal sealed record DemoMonitorSpec(
    string Name,
    string Url,
    string HttpMethod,
    int TimeoutSeconds,
    IReadOnlyList<string> Regions,
    IReadOnlyList<string> Tags);

internal sealed record DemoReliabilityProfile(
    double TypicalUptime,
    double DailyJitter,
    double IncidentChance,
    double IncidentMinimum,
    double IncidentMaximum,
    double UptimeSla);

internal static class DemoDataCatalog
{
    public static readonly IReadOnlyList<DemoTenantProfile> Tenants =
    [
        // B2B: high-value account and procurement ordering.
        new("W.W. Grainger", TenantType.B2B, "https://www.grainger.com", 4_000, 55_000, 18, 7, 1.10m, new(9_000m, 0.65, 28_000m, 0.12)),
        new("Fastenal", TenantType.B2B, "https://www.fastenal.com", 3_000, 42_000, 24, 9, 1.08m, new(7_000m, 0.60, 22_000m, 0.10)),
        new("Thermo Fisher Scientific", TenantType.B2B, "https://www.thermofisher.com", 12_000, 180_000, 8, 4, 1.04m, new(30_000m, 0.75, 110_000m, 0.15)),
        new("Siemens AG", TenantType.B2B, "https://www.siemens.com", 15_000, 220_000, 7, 4, 1.03m, new(38_000m, 0.80, 140_000m, 0.18)),
        new("Caterpillar", TenantType.B2B, "https://parts.cat.com", 9_000, 150_000, 9, 5, 1.06m, new(24_000m, 0.75, 90_000m, 0.15)),
        new("CDW Corporation", TenantType.B2B, "https://www.cdw.com", 6_000, 95_000, 20, 8, 1.16m, new(15_000m, 0.70, 52_000m, 0.12)),
        new("Ferguson", TenantType.B2B, "https://www.ferguson.com", 5_000, 70_000, 22, 9, 1.08m, new(12_000m, 0.65, 38_000m, 0.10)),
        new("MSC Industrial Supply", TenantType.B2B, "https://www.mscdirect.com", 3_500, 48_000, 25, 10, 1.09m, new(8_500m, 0.60, 26_000m, 0.10)),
        new("BASF", TenantType.B2B, "https://www.basf.com", 18_000, 260_000, 6, 3, 1.02m, new(45_000m, 0.75, 170_000m, 0.15)),
        new("Schneider Electric", TenantType.B2B, "https://www.se.com", 8_000, 125_000, 12, 5, 1.05m, new(22_000m, 0.70, 80_000m, 0.13)),

        // B2C: lower basket values and substantially higher order volume.
        new("ASOS", TenantType.B2C, "https://www.asos.com", 350, 3_200, 320, 75, 1.85m, new(700m, 0.42, 1_800m, 0.08)),
        new("H&M", TenantType.B2C, "https://www.hm.com", 300, 2_600, 430, 95, 1.75m, new(600m, 0.38, 1_500m, 0.06)),
        new("Sephora", TenantType.B2C, "https://www.sephora.com", 400, 3_800, 280, 65, 1.70m, new(900m, 0.45, 2_400m, 0.08)),
        new("Chewy", TenantType.B2C, "https://www.chewy.com", 450, 4_200, 245, 55, 1.25m, new(1_000m, 0.32, 2_500m, 0.10)),
        new("Wayfair", TenantType.B2C, "https://www.wayfair.com", 900, 12_000, 125, 38, 1.65m, new(2_500m, 0.65, 8_000m, 0.12)),
        new("Zalando", TenantType.B2C, "https://www.zalando.com", 450, 4_000, 260, 65, 1.80m, new(850m, 0.48, 2_300m, 0.08)),
        new("Decathlon", TenantType.B2C, "https://www.decathlon.com", 500, 5_500, 185, 48, 1.55m, new(1_100m, 0.55, 3_500m, 0.10)),
        new("Uniqlo", TenantType.B2C, "https://www.uniqlo.com", 350, 3_200, 235, 55, 1.65m, new(650m, 0.40, 1_800m, 0.06)),
        new("IKEA", TenantType.B2C, "https://www.ikea.com", 750, 14_000, 155, 42, 1.55m, new(2_200m, 0.65, 9_000m, 0.10)),
        new("Lululemon", TenantType.B2C, "https://www.lululemon.com", 700, 5_500, 115, 32, 1.60m, new(1_400m, 0.36, 3_500m, 0.08)),

        // Mixed: consumer storefronts plus wholesale or enterprise channels.
        new("Nike", TenantType.Mixed, "https://www.nike.com", 650, 9_000, 225, 58, 1.85m, new(1_300m, 0.55, 5_500m, 0.15)),
        new("Apple", TenantType.Mixed, "https://www.apple.com", 2_500, 65_000, 95, 28, 2.05m, new(7_000m, 0.60, 35_000m, 0.18)),
        new("Dell Technologies", TenantType.Mixed, "https://www.dell.com", 3_000, 85_000, 78, 24, 1.75m, new(9_000m, 0.70, 50_000m, 0.20)),
        new("Bosch", TenantType.Mixed, "https://www.bosch.com", 1_200, 35_000, 105, 32, 1.45m, new(3_500m, 0.65, 20_000m, 0.18)),
        new("LEGO", TenantType.Mixed, "https://www.lego.com", 450, 8_000, 185, 52, 2.30m, new(1_000m, 0.50, 4_500m, 0.12)),
        new("Sony", TenantType.Mixed, "https://www.sony.com", 1_800, 75_000, 82, 25, 1.90m, new(5_000m, 0.68, 40_000m, 0.15)),
        new("DJI", TenantType.Mixed, "https://www.dji.com", 2_500, 48_000, 58, 20, 1.80m, new(7_500m, 0.52, 30_000m, 0.15)),
        new("Stanley Black & Decker", TenantType.Mixed, "https://www.stanleyblackanddecker.com", 1_000, 28_000, 72, 24, 1.50m, new(3_000m, 0.60, 16_000m, 0.15)),
        new("L'Oréal", TenantType.Mixed, "https://www.loreal.com", 500, 16_000, 215, 58, 1.65m, new(1_400m, 0.58, 10_000m, 0.15)),
        new("Sennheiser", TenantType.Mixed, "https://www.sennheiser.com", 1_500, 32_000, 75, 22, 1.55m, new(4_500m, 0.52, 20_000m, 0.15))
    ];

    public static DemoTenantProfile? FindTenant(string name)
        => Tenants.FirstOrDefault(profile => profile.Name == name);

    public static IReadOnlyList<DemoMonitorSpec> GetMonitors(DemoTenantProfile tenant)
    {
        var commerceName = tenant.Type == TenantType.B2B ? "Procurement portal" : "Online store";
        var journeyName = tenant.Type switch
        {
            TenantType.B2B => "Account sign-in",
            TenantType.B2C => "Checkout journey",
            _ => "Partner portal"
        };
        var journeyPath = tenant.Type switch
        {
            TenantType.B2B => "account",
            TenantType.B2C => "checkout",
            _ => "business"
        };

        return
        [
            new(
                commerceName,
                tenant.BaseUrl,
                "HEAD",
                30,
                ["eu", "na"],
                ["PROD", "CUSTOMER-FACING"]),
            new(
                journeyName,
                CombineUrl(tenant.BaseUrl, journeyPath),
                "GET",
                45,
                tenant.Type == TenantType.B2C ? ["eu", "na", "apac"] : ["eu", "na"],
                ["PROD", tenant.Type == TenantType.B2B ? "B2B" : "COMMERCE"]),
            new(
                "Test storefront",
                BuildEnvironmentUrl(tenant.BaseUrl, "test", "smoke"),
                "GET",
                45,
                ["eu"],
                ["TEST", "QA", "INTERNAL"]),
            new(
                "Development API",
                BuildEnvironmentUrl(tenant.BaseUrl, "dev-api", "health"),
                "GET",
                20,
                ["eu"],
                ["DEV", "API", "INTERNAL"])
        ];
    }

    public static DemoReliabilityProfile GetReliability(int monitorOrdinal)
        => (Math.Abs(monitorOrdinal) % 20) switch
        {
            <= 13 => new(99.995, 0.004, 0.001, 99.50, 99.92, 99.90),
            <= 17 => new(99.975, 0.012, 0.003, 99.20, 99.85, 99.90),
            18 => new(99.930, 0.025, 0.008, 98.50, 99.65, 99.90),
            _ => new(99.800, 0.080, 0.015, 97.50, 99.35, 99.50)
        };

    public static double GenerateUptime(Random random, DemoReliabilityProfile reliability)
    {
        if (random.NextDouble() < reliability.IncidentChance)
        {
            return RandomBetween(random, reliability.IncidentMinimum, reliability.IncidentMaximum);
        }

        return Math.Clamp(
            reliability.TypicalUptime + RandomBetween(random, -reliability.DailyJitter, reliability.DailyJitter),
            0,
            100);
    }

    private static string CombineUrl(string baseUrl, string relativePath)
        => new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), relativePath).ToString().TrimEnd('/');

    private static string BuildEnvironmentUrl(string baseUrl, string subdomain, string relativePath)
    {
        var baseUri = new Uri(baseUrl);
        var productionHost = baseUri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? baseUri.Host[4..]
            : baseUri.Host;
        var builder = new UriBuilder(baseUri)
        {
            Host = $"{subdomain}.{productionHost}",
            Path = relativePath
        };

        return builder.Uri.ToString().TrimEnd('/');
    }

    private static double RandomBetween(Random random, double minimum, double maximum)
        => minimum + random.NextDouble() * (maximum - minimum);
}

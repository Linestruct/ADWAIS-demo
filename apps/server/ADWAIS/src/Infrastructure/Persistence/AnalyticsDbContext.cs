// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Adwais.Application.Common.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Adwais.Infrastructure.Persistence.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Adwais.Infrastructure.Persistence;

public class AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options, IDataProtectionProvider? dataProtectionProvider = null) 
    : DbContext(options), IApplicationDbContext
{
    public DbSet<GlobalConfig> GlobalConfigs => Set<GlobalConfig>();
    public DbSet<OrganizationConfig> OrganizationConfigs => Set<OrganizationConfig>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<UserAccess> UserAccesses => Set<UserAccess>();
    public DbSet<KioskDevice> KioskDevices => Set<KioskDevice>();
    public static readonly Guid SystemTenantGuid = new Guid("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SystemUserGuid = new Guid("00000000-0000-0000-0000-000000000002");
    public static readonly Guid DefaultOrganizationGuid = new Guid("00000000-0000-0000-0000-00000000000A");
    
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<DailyFinancialTenantRollup> DailyTenantRollups => Set<DailyFinancialTenantRollup>();
    public DbSet<DailyFinancialGlobalRollup> DailyGlobalRollups => Set<DailyFinancialGlobalRollup>();
    
    public DbSet<ResponseTime> ResponseTimes => Set<ResponseTime>();
    public DbSet<MonitorAvailability> MonitorAvailabilities => Set<MonitorAvailability>();
    public DbSet<UptimeMonitor> Monitors => Set<UptimeMonitor>();
    public DbSet<DailyLatencyMonitorRollup> DailyLatencyMonitorRollups => Set<DailyLatencyMonitorRollup>();
    public DbSet<DailyLatencyTenantRollup> DailyLatencyTenantRollups => Set<DailyLatencyTenantRollup>();
    public DbSet<DailyLatencyGlobalRollup> DailyLatencyGlobalRollups => Set<DailyLatencyGlobalRollup>();
    public DbSet<DailyAvailabilityMonitorRollup> DailyAvailabilityMonitorRollups => Set<DailyAvailabilityMonitorRollup>();
    public DbSet<DailyAvailabilityTenantRollup> DailyAvailabilityTenantRollups => Set<DailyAvailabilityTenantRollup>();
    public DbSet<DailyAvailabilityGlobalRollup> DailyAvailabilityGlobalRollups => Set<DailyAvailabilityGlobalRollup>();

    public DbSet<SystemEvent> SystemEvents => Set<SystemEvent>();
    public DbSet<PipelineRun> PipelineRuns => Set<PipelineRun>();
    public DbSet<MaterializedViewDirty> MaterializedViewDirty => Set<MaterializedViewDirty>();
    public DbSet<BulletinPost> BulletinPosts => Set<BulletinPost>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<CalendarSubscription> CalendarSubscriptions => Set<CalendarSubscription>();
    public DbSet<FeedSource> FeedSources => Set<FeedSource>();
    public DbSet<FeedItem> FeedItems => Set<FeedItem>();
    
    // intranät


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // NOTE: This project strictly uses Fluent API for all database schema mapping and properties configuration.
        // Do not add EF Core data annotations directly to the Domain models/entities.
        modelBuilder.HasPostgresExtension("uuid-ossp");

        // Force all DateTime and DateTimeOffset to UTC
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
                        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v));
                }
                else if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTimeOffset>(
                        v => v.Offset == TimeSpan.Zero ? v : v.ToUniversalTime(),
                        v => v));
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, DateTimeOffset?>(
                        v => v.HasValue ? (v.Value.Offset == TimeSpan.Zero ? v : v.Value.ToUniversalTime()) : v,
                        v => v));
                }
            }
        }

        // SystemEvent
        modelBuilder.Entity<SystemEvent>(entity =>
        {
            entity.ToTable("system_event");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.Code).HasMaxLength(100).IsRequired()
                .HasDefaultValue("legacy");
            entity.Property(e => e.Audience).StoreAsString().HasMaxLength(50)
                .HasDefaultValue(SystemEventAudience.Platform);
            entity.Property(e => e.TraceId).HasMaxLength(64);
            entity.Property(e => e.RequestId).HasMaxLength(200);
            entity.Property(e => e.SuggestedAction).HasMaxLength(500);
            entity.Property(e => e.Level).StoreAsString().HasMaxLength(50);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.OrganizationId, e.Timestamp, e.Id });
            entity.HasIndex(e => new { e.TenantId, e.Timestamp, e.Id });
        });

        // PipelineRun
        modelBuilder.Entity<PipelineRun>(entity =>
        {
            entity.ToTable("pipeline_run");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(e => e.Kind).StoreAsString().HasMaxLength(50);
            entity.Property(e => e.Trigger).StoreAsString().HasMaxLength(50);
            entity.Property(e => e.State).StoreAsString().HasMaxLength(50);
            entity.Property(e => e.ResourceKey).HasMaxLength(200);
            entity.Property(e => e.ResourceName).HasMaxLength(255);
            entity.Property(e => e.HangfireJobId).HasMaxLength(100);
            entity.Property(e => e.RequestId).HasMaxLength(200);
            entity.Property(e => e.TraceId).HasMaxLength(64);
            entity.Property(e => e.OutcomeCode).HasMaxLength(100);
            entity.Property(e => e.SafeSummary).HasMaxLength(500);
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.OrganizationId, e.RequestedAt, e.Id });
            entity.HasIndex(e => new { e.OrganizationId, e.TenantId, e.Kind, e.State });
        });

        // Tenant
        modelBuilder.Entity<Tenant>(entity => 
        {
            entity.ToTable("tenant");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(t => t.OrganizationId).IsRequired();
            entity.Property(t => t.Name).HasMaxLength(255);
            entity.Property(t => t.Type)
                .StoreAsString()
                .HasMaxLength(50)
                .HasDefaultValue(TenantType.Mixed);
            entity.Property(t => t.OrderProvider)
                .HasMaxLength(100)
                .HasDefaultValue(IntegrationProviders.Litium)
                .IsRequired();
            entity.Property(t => t.ImageUrl).HasMaxLength(2048);
            entity.Property(t => t.OrderProviderSettings).HasMaxLength(4096);
            if (dataProtectionProvider != null)
            {
                entity.Property(t => t.OrderProviderSettings)
                    .HasConversion(new EncryptedStringConverter(dataProtectionProvider));
            }
            entity.Property(t => t.CurrentlyFetching).HasDefaultValue(false);
            entity.Property(t => t.OrderFetchingEnabled).HasDefaultValue(false);
            entity.Property(t => t.IsSystem).HasDefaultValue(false);

            entity.HasOne(t => t.Organization)
                .WithMany(org => org.Tenants)
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // to catch unassigned monitors since TenantId is not nullable and a foreign key
            entity.HasData(
                new Tenant
                {
                    Id = SystemTenantGuid,
                    OrganizationId = DefaultOrganizationGuid,
                    Name = "System (unassigned monitors)",
                    Type = TenantType.Mixed,
                    OrderProviderSettings = null,
                    OrderFetchingEnabled = false,
                    IsSystem = true
                }
            );
        });
        
        // Order
        modelBuilder.Entity<Order>(entity => 
        {
            entity.ToTable("orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OrderState)
                .StoreAsString()
                .HasMaxLength(255);
            entity.Property(o => o.OrderNumber).HasMaxLength(255);
            entity.Property(o => o.Provider).HasMaxLength(100).IsRequired();
            entity.Property(o => o.ExternalId).HasMaxLength(255).IsRequired();
            entity.Property(o => o.Currency).HasMaxLength(10);

            entity.HasIndex(o => new { o.TenantId, o.Provider, o.ExternalId }).IsUnique();
            
            entity.HasIndex(o => new { o.TenantId, o.CreatedDate, o.OrderState });
                
            entity.HasIndex(o => new { o.CreatedDate, o.TenantId })
                .HasDatabaseName("idx_orders_composite_dash")
                .IncludeProperties(o => o.TotalValueIncVat);
                
            entity.HasIndex(o => new { o.TenantId, o.TotalValueIncVat })
                .HasDatabaseName("idx_orders_value_dist");
                
            entity.HasIndex(o => new { o.TenantId, o.CreatedDate })
                .HasDatabaseName("idx_orders_tenant_isolated")
                .IncludeProperties(o => o.TotalValueIncVat);
                
            entity.HasOne(o => o.Tenant)
                .WithMany(t => t.Orders)
                .HasForeignKey(o => o.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Rollups
        modelBuilder.Entity<DailyFinancialTenantRollup>(entity =>
        {
            entity.ToView("v_mat_financial_daily_tenant_rollup");
            entity.HasKey(r => new { r.CreatedDate, r.OrganizationId, r.TenantId });
        });

        modelBuilder.Entity<DailyFinancialGlobalRollup>(entity =>
        {
            entity.ToView("v_mat_financial_daily_global_rollup");
            entity.HasKey(r => new { r.CreatedDate, r.OrganizationId });
        });

        modelBuilder.Entity<DailyLatencyMonitorRollup>(entity =>
        {
            entity.ToView("v_mat_daily_latency_monitor_rollup");
            entity.HasKey(r => new { r.Date, r.OrganizationId, r.MonitorId });
            entity.HasOne(r => r.UptimeMonitor)
                .WithMany()
                .HasForeignKey(r => r.MonitorId);
        });

        modelBuilder.Entity<DailyLatencyTenantRollup>(entity =>
        {
            entity.ToView("v_mat_daily_latency_tenant_rollup");
            entity.HasKey(r => new { r.Date, r.OrganizationId, r.TenantId });
            entity.HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId);
        });

        modelBuilder.Entity<DailyLatencyGlobalRollup>(entity =>
        {
            entity.ToView("v_mat_daily_latency_global_rollup");
            entity.HasKey(r => new { r.Date, r.OrganizationId });
        });

        modelBuilder.Entity<DailyAvailabilityMonitorRollup>(entity =>
        {
            entity.ToView("v_mat_daily_availability_monitor_rollup");
            entity.HasKey(r => new { r.Date, r.OrganizationId, r.MonitorId });
            entity.HasOne(r => r.UptimeMonitor)
                .WithMany()
                .HasForeignKey(r => r.MonitorId);
        });

        modelBuilder.Entity<DailyAvailabilityTenantRollup>(entity =>
        {
            entity.ToView("v_mat_daily_availability_tenant_rollup");
            entity.HasKey(r => new { r.Date, r.OrganizationId, r.TenantId });
            entity.HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId);
        });

        modelBuilder.Entity<DailyAvailabilityGlobalRollup>(entity =>
        {
            entity.ToView("v_mat_daily_availability_global_rollup");
            entity.HasKey(r => new { r.Date, r.OrganizationId });
        });
        
        // MaterializedViewDirty
        modelBuilder.Entity<MaterializedViewDirty>(entity =>
        {
            entity.ToTable("materialized_view_dirty");
            entity.HasKey(e => e.OrganizationId);
            entity.Property(e => e.MarkedAt).IsRequired();
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // GlobalConfig
        modelBuilder.Entity<GlobalConfig>(entity => 
        {
            entity.ToTable("global_config", t =>
                t.HasCheckConstraint("CK_GlobalConfig_SingleRow", 
                    "\"id\" = 1"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SystemEventRetentionDays).HasDefaultValue(2);
            entity.Property(x => x.MatViewRefreshIntervalMinutes).HasDefaultValue(60);
            entity.Property(x => x.VisibleRecurringJobsCsv)
                .HasDefaultValueSql($"'{Adwais.Application.Common.Jobs.RecurringJobVisibility.DefaultVisiblePlatformJobs}'");

            entity.HasData(new GlobalConfig
            {
                Id = 1,
                SystemEventRetentionDays = 2,
                MatViewRefreshIntervalMinutes = 60,
                VisibleRecurringJobsCsv = Adwais.Application.Common.Jobs.RecurringJobVisibility.DefaultVisiblePlatformJobs
            });
        });
        
        // User
        modelBuilder.Entity<User>(entity => 
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasDefaultValueSql("uuid_generate_v4()");
            
            entity.Property(u => u.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(255)
                .IsRequired(false);

            entity.Property(u => u.ExternalSubjectId)
                .HasMaxLength(255)
                .IsRequired(false);

            entity.HasIndex(u => u.ExternalSubjectId)
                .IsUnique();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.CalendarFeedToken)
                .HasMaxLength(255)
                .IsRequired(false);

            entity.HasIndex(u => u.CalendarFeedToken)
                .IsUnique();

            entity.HasData(new User
            {
                Id = SystemUserGuid,
                Name = "System",
                Email = "system@adwais.local"
            });
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organization");
            entity.HasKey(org => org.Id);
            entity.Property(org => org.Id)
                .HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(org => org.Name)
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(org => org.CreatedAt)
                .IsRequired();

            entity.HasData(
                new Organization
                {
                    Id = DefaultOrganizationGuid,
                    Name = "Default Organization",
                    CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                }
            );
        });

        modelBuilder.Entity<OrganizationConfig>(entity =>
        {
            entity.ToTable("organization_config");
            entity.HasKey(config => config.OrganizationId);
            entity.Property(config => config.WeatherLocation).HasMaxLength(255);
            entity.Property(config => config.ReportingTimeZoneId).HasMaxLength(100).HasDefaultValue("Europe/Stockholm");
            entity.Property(config => config.MonitoringProvider).HasMaxLength(50).HasDefaultValue(IntegrationProviders.UptimeRobot);
            entity.Property(config => config.MonitoringProviderSettings).HasMaxLength(4096);
            if (dataProtectionProvider != null)
            {
                entity.Property(config => config.MonitoringProviderSettings)
                    .HasConversion(new EncryptedStringConverter(dataProtectionProvider));
            }
            entity.Property(config => config.WeatherFetchIntervalMinutes).HasDefaultValue(15);
            entity.Property(config => config.OrderFetchEnabled).HasDefaultValue(true);
            entity.Property(config => config.MonitoringFetchEnabled).HasDefaultValue(true);
            entity.Property(config => config.OrderFetchIntervalMinutes).HasDefaultValue(60);
            entity.Property(config => config.UptimeFetchIntervalMinutes).HasDefaultValue(60);
            entity.Property(config => config.LatencyFetchIntervalMinutes).HasDefaultValue(10);
            entity.Property(config => config.UserStatsFetchIntervalMinutes).HasDefaultValue(60);
            entity.Property(config => config.FeedFetchIntervalHours).HasDefaultValue(2);

            entity.HasOne(config => config.Organization)
                .WithOne()
                .HasForeignKey<OrganizationConfig>(config => config.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAccess>(entity =>
        {
            entity.ToTable("user_access", table =>
            {
                // The platform shape is structural: a null organization only
                // ever pairs with the PlatformAdmin role, and the role never
                // pairs with an organization. The database rejects every other
                // combination, so no write path can bypass the invariant.
                table.HasCheckConstraint(
                    "ck_user_access_role",
                    $"""
                    {DbEnum.CheckConstraintSql<Adwais.Domain.Enums.UserRole>("role")}
                    AND (
                      ("organization_id" IS NULL AND "role" = '{nameof(Adwais.Domain.Enums.UserRole.PlatformAdmin)}')
                      OR ("organization_id" IS NOT NULL AND "role" <> '{nameof(Adwais.Domain.Enums.UserRole.PlatformAdmin)}')
                    )
                    """);
            });
            entity.HasKey(access => access.Id);
            entity.Property(access => access.Id)
                .HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(access => access.Role)
                .StoreAsString()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(access => access.CreatedAt)
                .IsRequired();

            entity.HasOne(access => access.User)
                .WithMany()
                .HasForeignKey(access => access.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(access => access.Organization)
                .WithMany(org => org.UserAccesses)
                .HasForeignKey(access => access.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(access => access.Tenant)
                .WithMany()
                .HasForeignKey(access => access.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(access => access.UserId);
            entity.HasIndex(access => access.OrganizationId);
        });
        
        modelBuilder.Entity<KioskDevice>(entity =>
        {
            entity.ToTable("kiosk_devices");
            entity.HasKey(kd => kd.Id);
            entity.Property(kd => kd.Id)
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(kd => kd.DeviceId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(kd => kd.ActivationCode)
                .HasMaxLength(6)
                .IsRequired();

            entity.Property(kd => kd.ActivationCodeExpires)
                .IsRequired();

            entity.Property(kd => kd.IsAuthorized)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(kd => kd.AuthorizedAt)
                .IsRequired(false);

            entity.Property(kd => kd.CreatedDate)
                .IsRequired();
            
            entity.Property(kd => kd.OrganizationId)
                .IsRequired(false);
            entity.HasOne<Organization>()
                .WithMany()
                .HasForeignKey(kd => kd.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(kd => kd.DeviceId).IsUnique();
            entity.HasIndex(kd => kd.ActivationCode);
        });
            
        // ResponseTime
        modelBuilder.Entity<ResponseTime>(entity => 
        {
            entity.ToTable("response_time");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.HasOne(rt => rt.UptimeMonitor)
                .WithMany(m => m.ResponseTimes)
                .HasForeignKey(rt => rt.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MonitorAvailability
        modelBuilder.Entity<MonitorAvailability>(entity =>
        {
            entity.ToTable("monitor_availability");
            entity.HasKey(ma => ma.Id);
            entity.Property(ma => ma.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(ma => ma.IsFinalized).HasDefaultValue(false);
            entity.HasOne(ma => ma.UptimeMonitor)
                .WithMany()
                .HasForeignKey(ma => ma.MonitorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(ma => new { ma.MonitorId, ma.Date }).IsUnique();
        });
            
        // UptimeMonitor
        modelBuilder.Entity<UptimeMonitor>(entity => 
        {
            entity.ToTable("monitor");
            entity.HasKey(m => m.Id);
            entity.Ignore(m => m.StatusStr);
            entity.Ignore(m => m.CurrentUptimePercentage);
            entity.Ignore(m => m.CurrentLatency);
            entity.Property(m => m.Type)
                .HasMaxLength(50)
                .HasDefaultValue(UptimeMonitorTypes.Http)
                .IsRequired();
            entity.Property(m => m.Provider).HasMaxLength(100).IsRequired();
            entity.Property(m => m.ExternalId).HasMaxLength(255).IsRequired();
            entity.Property(m => m.Name).HasMaxLength(255);
            entity.Property(m => m.Url).HasMaxLength(2048);
            entity.Property(m => m.HttpMethod).HasMaxLength(50);
            entity.Property(m => m.LastIncidentId).HasMaxLength(100);
            entity.Property(m => m.LastIncidentStatus).HasMaxLength(100);
            entity.Property(m => m.LastIncidentCause).HasMaxLength(255);
            entity.Property(m => m.LastIncidentReason).HasMaxLength(2000);
            entity.HasOne(m => m.Tenant)
                .WithMany(t => t.Monitors)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(m => m.UpdateInterval).HasDefaultValue(300);
            entity.Property(m => m.Tags).HasDefaultValueSql("'{}'");
            entity.Property(m => m.MonitoredRegions).HasDefaultValueSql("'{}'");
            entity.HasIndex(m => new { m.TenantId, m.Provider, m.ExternalId }).IsUnique();
        });
            
        // intranät
        
        // BulletinPost
        modelBuilder.Entity<BulletinPost>(entity =>
        {
            entity.ToTable("bulletin_post");
            entity.HasKey(cp => cp.Id);
            entity.Property(cp => cp.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(cp => cp.Title).HasMaxLength(255).IsRequired();
            entity.Property(cp => cp.Body).IsRequired();
            entity.HasOne(cp => cp.User)
                .WithMany()
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cp => cp.Organization)
                .WithMany()
                .HasForeignKey(cp => cp.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(cp => cp.UserId);
            entity.HasIndex(cp => cp.OrganizationId);
            entity.HasIndex(cp => cp.CreatedAt);
        });

        // CalendarEvent
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.ToTable("calendar_event", table =>
            {
                table.HasCheckConstraint(
                    "ck_calendar_event_event_type",
                    DbEnum.CheckConstraintSql<Adwais.Domain.Enums.EventType>("event_type"));
                table.HasCheckConstraint(
                    "ck_calendar_event_recurrence",
                    DbEnum.CheckConstraintSql<Adwais.Domain.Enums.RecurrenceType>("recurrence"));
            });
            entity.HasKey(oe => oe.Id);
            entity.Property(oe => oe.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(oe => oe.Title).HasMaxLength(255).IsRequired();
            entity.Property(oe => oe.Description).IsRequired(false);
            entity.Property(oe => oe.Location).HasMaxLength(255).IsRequired(false);
            entity.Property(oe => oe.EventType)
                .StoreAsString()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(EventType.General);
            entity.Property(oe => oe.Recurrence)
                .StoreAsString()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(RecurrenceType.None);
            entity.Property(oe => oe.ExternalUid).HasMaxLength(255).IsRequired(false);
            entity.HasOne(oe => oe.User)
                .WithMany()
                .HasForeignKey(oe => oe.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(oe => oe.CalendarSubscription)
                .WithMany(cs => cs.Events)
                .HasForeignKey(oe => oe.CalendarSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(oe => oe.Organization)
                .WithMany()
                .HasForeignKey(oe => oe.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(oe => oe.OrganizationId);
            entity.HasIndex(oe => oe.StartTime);
            entity.HasIndex(oe => oe.EndTime);
            entity.HasIndex(oe => oe.ExternalUid);
        });

        // CalendarSubscription
        modelBuilder.Entity<CalendarSubscription>(entity =>
        {
            entity.ToTable("calendar_subscription");
            entity.HasKey(cs => cs.Id);
            entity.Property(cs => cs.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(cs => cs.Name).HasMaxLength(255).IsRequired();
            entity.Property(cs => cs.Url).HasMaxLength(2048).IsRequired();
            entity.Property(cs => cs.IsActive).HasDefaultValue(true);
            entity.Property(cs => cs.LastSyncError).HasMaxLength(4000);
            entity.HasOne(cs => cs.Organization)
                .WithMany()
                .HasForeignKey(cs => cs.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(cs => cs.OrganizationId);
        });

        // FeedSource
        modelBuilder.Entity<FeedSource>(entity =>
        {
            entity.ToTable("feed_source");
            entity.HasKey(fs => fs.Id);
            entity.Property(fs => fs.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(fs => fs.Name).HasMaxLength(255).IsRequired();
            entity.Property(fs => fs.Url).HasMaxLength(2048).IsRequired();
            entity.HasOne(fs => fs.Organization)
                .WithMany()
                .HasForeignKey(fs => fs.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(fs => new { fs.OrganizationId, fs.Url }).IsUnique();
            entity.Property(fs => fs.LastSuccessAt);
            entity.Property(fs => fs.LastSyncError).HasMaxLength(4000);
        });



        // FeedItem
        modelBuilder.Entity<FeedItem>(entity =>
        {
            entity.ToTable("feed_item");
            entity.HasKey(fi => fi.Id);
            entity.Property(fi => fi.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(fi => fi.Title).HasMaxLength(512).IsRequired();
            entity.Property(fi => fi.Content).IsRequired(false);
            entity.Property(fi => fi.Link).HasMaxLength(2048).IsRequired();
            entity.Property(fi => fi.Author).HasMaxLength(255).IsRequired(false);
            entity.Property(fi => fi.ImageUrl).HasMaxLength(2048).IsRequired(false);
            entity.HasOne(fi => fi.FeedSource)
                .WithMany(fs => fs.FeedItems)
                .HasForeignKey(fi => fi.FeedSourceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(fi => fi.Link).IsUnique();
            entity.HasIndex(fi => fi.PublishDate);
        });
    }
}


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
    public DbSet<User> Users => Set<User>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<KioskDevice> KioskDevices => Set<KioskDevice>();
    public static readonly Guid SystemTenantGuid = new Guid("00000000-0000-0000-0000-000000000001");
    public static readonly Guid SystemUserGuid = new Guid("00000000-0000-0000-0000-000000000002");
    
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
            entity.Property(e => e.Level).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Timestamp);
        });

        // Tenant
        modelBuilder.Entity<Tenant>(entity => 
        {
            entity.ToTable("tenant");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(t => t.Name).HasMaxLength(255);
            entity.Property(t => t.Type)
                .HasConversion<string>()
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

            // to catch unassigned monitors since TenantId is not nullable and a foreign key
            entity.HasData(
                new Tenant
                {
                    Id = SystemTenantGuid,
                    Name = "System (unassigned monitors)",
                    Type = TenantType.Mixed,
                    OrderProviderSettings = null,
                    OrderFetchingEnabled = false
                }
            );
        });
        
        // Order
        modelBuilder.Entity<Order>(entity => 
        {
            entity.ToTable("orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OrderState)
                .HasConversion<string>()
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
            entity.HasKey(r => new { r.CreatedDate, r.TenantId });
        });
        
        modelBuilder.Entity<DailyFinancialGlobalRollup>(entity => 
        {
            entity.ToView("v_mat_financial_daily_global_rollup");
            entity.HasKey(r => r.CreatedDate);
        });
        
        modelBuilder.Entity<DailyLatencyMonitorRollup>(entity => 
        {
            entity.ToView("v_mat_daily_latency_monitor_rollup");
            entity.HasKey(r => new { r.Date, r.MonitorId });
            entity.HasOne(r => r.UptimeMonitor)
                .WithMany()
                .HasForeignKey(r => r.MonitorId);
        });
        
        modelBuilder.Entity<DailyLatencyTenantRollup>(entity => 
        {
            entity.ToView("v_mat_daily_latency_tenant_rollup");
            entity.HasKey(r => new { r.Date, r.TenantId });
            entity.HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId);
        });
        
        modelBuilder.Entity<DailyLatencyGlobalRollup>(entity => 
        {
            entity.ToView("v_mat_daily_latency_global_rollup");
            entity.HasKey(r => r.Date);
        });

        modelBuilder.Entity<DailyAvailabilityMonitorRollup>(entity =>
        {
            entity.ToView("v_mat_daily_availability_monitor_rollup");
            entity.HasKey(r => new { r.Date, r.MonitorId });
            entity.HasOne(r => r.UptimeMonitor)
                .WithMany()
                .HasForeignKey(r => r.MonitorId);
        });

        modelBuilder.Entity<DailyAvailabilityTenantRollup>(entity =>
        {
            entity.ToView("v_mat_daily_availability_tenant_rollup");
            entity.HasKey(r => new { r.Date, r.TenantId });
            entity.HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantId);
        });

        modelBuilder.Entity<DailyAvailabilityGlobalRollup>(entity =>
        {
            entity.ToView("v_mat_daily_availability_global_rollup");
            entity.HasKey(r => r.Date);
        });
        
        // GlobalConfig
        modelBuilder.Entity<GlobalConfig>(entity => 
        {
            entity.ToTable("global_config", t =>
                t.HasCheckConstraint("CK_GlobalConfig_SingleRow", 
                    "\"id\" = 1"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MonitoringProvider)
                .HasMaxLength(100)
                .HasDefaultValue(IntegrationProviders.UptimeRobot)
                .IsRequired();
            entity.Property(x => x.MonitoringProviderSettings).HasMaxLength(4096);
            if (dataProtectionProvider != null)
            {
                entity.Property(x => x.MonitoringProviderSettings)
                    .HasConversion(new EncryptedStringConverter(dataProtectionProvider));
            }
            entity.Property(x => x.UptimeFetchIntervalMinutes).HasDefaultValue(60);
            entity.Property(x => x.LatencyFetchIntervalMinutes).HasDefaultValue(10);
            entity.Property(x => x.UserStatsFetchIntervalMinutes).HasDefaultValue(60);
            entity.Property(x => x.SystemEventRetentionDays).HasDefaultValue(2);
            entity.Property(x => x.OrderFetchEnabled).HasDefaultValue(true);
            entity.Property(x => x.MonitoringFetchEnabled).HasDefaultValue(true);
            entity.Property(x => x.WeatherLocation).HasDefaultValue("Karlstad");
            entity.Property(x => x.WeatherFetchIntervalMinutes).HasDefaultValue(15);
            entity.Property(x => x.ReportingTimeZoneId).HasMaxLength(100).HasDefaultValue("Europe/Stockholm");

            entity.HasData(new GlobalConfig
            {
                Id = 1,
                OrderFetchEnabled = true,
                MonitoringFetchEnabled = true,
                OrderFetchIntervalMinutes = 60,
                UptimeFetchIntervalMinutes = 60,
                LatencyFetchIntervalMinutes = 10,
                UserStatsFetchIntervalMinutes = 60,
                SystemEventRetentionDays = 2,
                FeedFetchIntervalHours = 2,
                WeatherLocation = "Karlstad",
                WeatherFetchIntervalMinutes = 15,
                ReportingTimeZoneId = "Europe/Stockholm"
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

            entity.Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(50)
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
                Role = UserRole.Employee,
                Email = "system@adwais.local"
            });
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
            entity.HasIndex(cp => cp.UserId);
            entity.HasIndex(cp => cp.CreatedAt);
        });

        // CalendarEvent
        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.ToTable("calendar_event", table =>
            {
                table.HasCheckConstraint(
                    "ck_calendar_event_event_type",
                    "\"event_type\" IN ('General', 'Meeting', 'Fika', 'Social', 'Birthday', 'GoLive', 'ExternalSync')");
                table.HasCheckConstraint(
                    "ck_calendar_event_recurrence",
                    "\"recurrence\" IN ('None', 'Daily', 'Weekly', 'Monthly', 'Yearly')");
            });
            entity.HasKey(oe => oe.Id);
            entity.Property(oe => oe.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(oe => oe.Title).HasMaxLength(255).IsRequired();
            entity.Property(oe => oe.Description).IsRequired(false);
            entity.Property(oe => oe.Location).HasMaxLength(255).IsRequired(false);
            entity.Property(oe => oe.EventType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue(EventType.General);
            entity.Property(oe => oe.Recurrence)
                .HasConversion<string>()
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
        });

        // FeedSource
        modelBuilder.Entity<FeedSource>(entity =>
        {
            entity.ToTable("feed_source");
            entity.HasKey(fs => fs.Id);
            entity.Property(fs => fs.Id).HasDefaultValueSql("uuid_generate_v4()");
            entity.Property(fs => fs.Name).HasMaxLength(255).IsRequired();
            entity.Property(fs => fs.Url).HasMaxLength(2048).IsRequired();
            entity.HasIndex(fs => fs.Url).IsUnique();
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


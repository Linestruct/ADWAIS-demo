// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Tenants;
using Adwais.Api.DTOs.Ingestion;
using Adwais.Api.Exceptions;
using DotNetEnv;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Storage;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Infrastructure;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Helpers;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.MaterializedViews;
using Adwais.Infrastructure.Jobs.Monitor;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Adwais.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
builder.Configuration.AddEnvironmentVariables();

var isBuildTime = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Adwais.Api.Filters.ValidationFilter>();
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new Adwais.Api.Converters.DecimalRoundingConverter());
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHttpClient();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppAuthentication(builder.Configuration);

builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<IMonitorOrchestrationService, MonitorOrchestrationService>();

builder.Services.AddDataProtection()
    .SetApplicationName("ADWAIS")
    .PersistKeysToFileSystem(new DirectoryInfo("/app/dp-keys"));

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1Ni...\""
    });

    options.AddSecurityRequirement(doc => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc, null),
            new List<string>()
        }
    });
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();

var connectionString = builder.Configuration.GetConnectionString("AnalyticsDb");

if (!isBuildTime)
{
    builder.Services.AddHangfire(config =>
    {
        config.UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(connectionString);
            var storageOptions = new PostgreSqlStorageOptions();
            storageOptions.SchemaName = "hangfire";
            storageOptions.PrepareSchemaIfNecessary = true;
        });
    });
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<Adwais.Api.Middleware.DevMockAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

if (!isBuildTime)
{
    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new Adwais.Api.Filters.AdminDashboardAuthorizationFilter() }
    });
}

app.MapControllers();

if (!isBuildTime)
{
    await app.BootstrapApplicationAsync();
}

app.Run();



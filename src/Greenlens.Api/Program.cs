using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlens.Api.Extensions;
using Greenlens.Api.Swagger;
using Greenlens.Api.Middlewares;
using Greenlens.Infrastructure;
using Greenlens.Infrastructure.Seeders.Administrator;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Greenlens.Infrastructure.Notifications.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// ── Infrastructure (DB, Auth, MediatR, etc.) ─────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── P0 performance: rate limit (BR-SYS-004) + Brotli compression ──
builder.Services.AddGreenlensPerformance(builder.Configuration);

// ── Health checks (Docker healthcheck + Tunnel smoke test) ──
builder.Services.AddHealthChecks();

// ── CORS ─────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Thay cho AllowAnyOrigin để dùng được AllowCredentials
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Bắt buộc đối với SignalR khi FE gọi từ domain khác
    });
});

// ── Idempotency (Idempotency-Key header replay) ─────
builder.Services.AddGreenlensIdempotency();

// ── Controllers ──────────────────────────────────────
builder.Services.AddControllers(options =>
    {
        // Override default [ApiController] model binding error response
        // to return ApiResponse instead of ValidationProblemDetails
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => new
                {
                    field = e.Key,
                    message = e.Value!.Errors.First().ErrorMessage
                })
                .ToList();

            var response = new Greenlens.Application.Common.Models.ApiResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "Dữ liệu đầu vào không hợp lệ.",
                Status = 400,
                Data = new { errors }
            };

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

// ── Swagger ──────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.OperationFilter<MultipartFormFileOperationFilter>();
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "GreenLens API",
        Version = "v1",
        Description = "Crowdsourced Application for Reporting Environmental Pollution"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Tag ordering — FE đọc Swagger theo thứ tự dashboard
    var tagOrder = new[]
    {
        "📋 Reports — Citizen Flow",
        "🔐 Auth — Authentication",
        "👤 Users — User Profile",
        "🔍 DEO Dashboard",
        "📌 LEO Dashboard",
        "🧹 Cleaner Dashboard",
        "🔎 Inspector Dashboard",
        "⚙️ Admin Dashboard",
        "📚 Catalog — Reference Data",
        "🗺️ Map — Public Map",
        "📎 Media — File Upload"
    };
    options.OrderActionsBy(apiDesc =>
    {
        var tag = apiDesc.ActionDescriptor.EndpointMetadata
            .OfType<TagsAttribute>()
            .FirstOrDefault()?.Tags.FirstOrDefault() ?? "zzz";
        var idx = Array.IndexOf(tagOrder, tag);
        return $"{(idx >= 0 ? idx : 99):D2}_{tag}_{apiDesc.RelativePath}";
    });
});

var app = builder.Build();

// ── Forwarded headers (Cloudflare Tunnel / reverse proxy) ──
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ── Middleware pipeline ──────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ── Auto migrate database on startup ──
await app.Services.MigrateDatabaseAsync();

// Swagger enabled in all environments (FE team needs API docs on VPS)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GreenLens API v1");
    c.RoutePrefix = "swagger";
});

app.UseResponseCompression();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

// ── Hangfire Dashboard (admin only in production) ──
app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
{
    Authorization = [] // TODO: add admin-only auth filter for production
});
app.UseRecurringJobs();

app.Run();

// chạy ở terminal trong thư mục gốc
// dotnet ef migrations add <TenMigration> --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api
// dotnet ef database update --project src/Greenlens.Infrastructure --startup-project src/Greenlens.Api

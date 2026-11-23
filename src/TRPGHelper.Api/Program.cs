using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using TRPGHelper.Api.Data;
using TRPGHelper.Api.Endpoints;
using TRPGHelper.Api.Extensions;
using TRPGHelper.Infrastructure;
using TRPGHelper.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Length == 0)
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }
    else
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    }
}));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
    .ExcludeFromDescription();

app.MapGet("/api", () => Results.Ok(new
{
    name = "TRPGHelper API",
    description = "电子卡器：记录、留存、辅助车卡",
    version = "1.0.0",
    docs = "/scalar/v1",
}));

app.MapAuthEndpoints();
app.MapSystemEndpoints();
app.MapCharacterEndpoints();
app.MapGeneratorEndpoints();

await app.InitializeDatabaseAsync();

app.Run();

/// <summary>给集成测试用的入口标记。</summary>
public partial class Program;

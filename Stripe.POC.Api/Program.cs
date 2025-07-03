using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using POC.Api.Common;
using POC.Api.Persistence;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.Configure<StripeConfig>(builder.Configuration.GetSection("Stripe"));

var stripe = new StripeConfig { ApiKey = string.Empty, ReturnUrl = string.Empty };
builder.Configuration.Bind("Stripe", stripe);
StripeConfiguration.ApiKey = stripe.ApiKey;
StripeConfiguration.AddBetaVersion("checkout_server_update_beta", "v1");

// Register the DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppContext")));

builder.Services.AddScoped<StripeSessionService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "POC.Api", Version = "v1" });

    // Add custom schema ID generator to handle nested classes
    c.CustomSchemaIds(type =>
    {
        var fullName = type.FullName?.Replace("+", ".");
        return fullName ?? type.Name;
    });


    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddAuthorization();
builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = builder.Configuration["JWT:SigningKey"]);

builder.Services.ConfigureHttpJsonOptions(options => { options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

builder.Services.AddCors(opt => { opt.AddPolicy("CorsPolicy", b => { b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); }); });
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(x => x.AutoTagPathSegmentIndex = 0);
var app = builder.Build();

app.UseCors("CorsPolicy");
app.UseFastEndpoints()
    .UseSwaggerGen();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
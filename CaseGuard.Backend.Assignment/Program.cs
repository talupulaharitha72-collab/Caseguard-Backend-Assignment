using System.Text;
using CaseGuard.Backend.Assignment.Data;
using CaseGuard.Backend.Assignment.Jobs;
using CaseGuard.Backend.Assignment.Middleware;
using CaseGuard.Backend.Assignment.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<LicenseService>();
builder.Services.AddHostedService<LicenseRenewalJob>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference(opt =>
{
    opt.Title = "CaseGuard API";
    opt.AddHttpAuthentication("Bearer", b => b.Token = "");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();

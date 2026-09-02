using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using smp_trask1.Data;
using smp_trask1.Handlers;
using smp_trask1.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
                .AddNewtonsoftJson();
builder.Services.AddDbContext<SmpDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.Zero
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthHandler>();
builder.Services.AddOpenApi();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped< PasswordHashService>();
builder.Services.AddScoped<UserRegister>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<EmployeeAttendanceService>();
builder.Services.AddScoped<StudentRegistrationService>();
builder.Services.AddScoped<StudentAttendanceService>();
builder.Services.AddScoped<SubjectService>();
builder.Services.AddScoped<StudentMarksService>();
builder.Services.AddScoped<StudentMarksReportService>();
builder.Services.AddScoped<LocationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "School Management API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

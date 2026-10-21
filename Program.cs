using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ShelfApi.Infrastructure;
using ShelfApi.Repositories;
using ShelfApi.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSingleton<IDbConnectionFactory>(
    new MySqlConnectionFactory(connectionString!)
); 

// Repositories
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IFornecedorRepository, FornecedorRepository>();
builder.Services.AddScoped<IKitRepository, KitRepository>();
builder.Services.AddScoped<IKitItemRepository, KitItemRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ISoftwareRepository, SoftwareRepository>();
builder.Services.AddScoped<IEstoqueRepository, EstoqueRepository>();
builder.Services.AddScoped<IEstoqueKitRepository, EstoqueKitRepository>();

// Services
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IFornecedorService, FornecedorService>();
builder.Services.AddScoped<IKitService, KitService>();
builder.Services.AddScoped<IKitItemService, KitItemService>();
builder.Services.AddScoped<ISoftwareService, SoftwareService>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<IEstoqueKitService, EstoqueKitService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Autenticação JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ShelfAppSuperSecretKeyForJwtAuthentication2026Enterprise!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ShelfApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ShelfApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e =>
                {
                    var firstError = e.Value!.Errors.First();
                    var errorMsg = firstError.ErrorMessage;
                    if (string.IsNullOrWhiteSpace(errorMsg) && firstError.Exception != null)
                    {
                        errorMsg = firstError.Exception.Message;
                    }

                    if (errorMsg.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
                        errorMsg.Contains("is not valid", StringComparison.OrdinalIgnoreCase) ||
                        errorMsg.Contains("JSON value", StringComparison.OrdinalIgnoreCase))
                    {
                        var fieldClean = e.Key.TrimStart('$', '.');
                        return $"O valor fornecido para '{fieldClean}' é inválido ou excede o limite numérico permitido.";
                    }

                    return errorMsg;
                })
                .ToList();

            var message = errors.Count == 1 ? errors[0] : string.Join(" ", errors);
            return new BadRequestObjectResult(new { message });
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
using BancoAPI.Api.Data;
using BancoAPI.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURAÇÃO DOS SERVIÇOS
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddEndpointsApiExplorer();

// Serviços do Banco
builder.Services.AddScoped<ContaService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Banco Único API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Digite: Bearer {seu_token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

// ============================================================
// 2. CONEXÃO COM BANCO DE DADOS (MYSQL)
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BancoDigitalDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// ============================================================
// 3. CONFIGURAÇÃO DE SEGURANÇA (JWT)
// ============================================================
var key = builder.Configuration["Jwt:Key"] ?? "SistemaBancarioChaveSecretaForte2024!@#$%XYZ";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "BancoAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "BancoAPIUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ============================================================
// 4. CONFIGURAÇÃO DO PIPELINE (ORDEM IMPORTANTE)
// ============================================================

// Habilita o Front-end (wwwroot) como prioridade máxima
app.UseDefaultFiles();
app.UseStaticFiles();

// Swagger configurado para não "atropelar" a página inicial
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Banco Único API v1");
    // Removido o RoutePrefix = string.Empty para liberar a home para o HTML
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ==============================================================================
// 5. BLOCO DE CRIAÇÃO AUTOMÁTICA E SEED
// ==============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BancoDigitalDbContext>();
        context.Database.EnsureCreated();

        if (!context.Agencias.Any())
        {
            var agencia = new BancoAPI.Api.Models.Agencia
            {
                NomeAgencia = "Agência Central Salvador",
                Endereco = "Av. Orlando Gomes, 1845 - SENAI CIMATEC",
                Cidade = "Salvador",
                Estado = "BA"
            };
            context.Agencias.Add(agencia);
            context.SaveChanges();
        }

        if (!context.Clientes.Any())
        {
            string senhaCriptografada = BCrypt.Net.BCrypt.HashPassword("12345");
            var idAgencia = context.Agencias.First().Id;

            var c1 = new BancoAPI.Api.Models.Cliente { Nome = "Gabriel Cunha", Cpf = "11122233344", Email = "gabriel@bancounico.com", SenhaHash = senhaCriptografada };
            var c2 = new BancoAPI.Api.Models.Cliente { Nome = "Marcelo Dias", Cpf = "55566677788", Email = "marcelo@bancounico.com", SenhaHash = senhaCriptografada };

            context.Clientes.AddRange(c1, c2);
            context.SaveChanges();

            context.Contas.AddRange(
                new BancoAPI.Api.Models.Conta { ClienteId = c1.Id, AgenciaId = idAgencia, NumeroConta = "1001-X", Saldo = 1000000m, TipoConta = "Corrente", StatusConta = "Ativa", DataAbertura = DateTime.Now },
                new BancoAPI.Api.Models.Conta { ClienteId = c2.Id, AgenciaId = idAgencia, NumeroConta = "1002-X", Saldo = 1000000m, TipoConta = "Corrente", StatusConta = "Ativa", DataAbertura = DateTime.Now }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> ERRO NA INICIALIZAÇÃO: {ex.Message}");
    }
}

// Configuração de Porta Dinâmica para a Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
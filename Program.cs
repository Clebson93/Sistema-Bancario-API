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
builder.Services.AddScoped<ContaService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BancoAPI", Version = "v1" });
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

// ==============================================================================
// 🔥 4. BLOCO DE CRIAÇÃO AUTOMÁTICA E SEED (AGÊNCIA -> CLIENTES -> CONTAS)
// ==============================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BancoDigitalDbContext>();

        // Garante que o banco e as tabelas existam (Resolve erro de Unknown Database)
        context.Database.EnsureCreated();

        // A. Criar Agência se não existir (Evita erro de login e fechamento)
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
            Console.WriteLine(">>> Agência Central criada!");
        }

        var idAgencia = context.Agencias.First().Id;

        // B. Criar Clientes com Senha Criptografada (Para o AuthController validar)
        if (!context.Clientes.Any())
        {
            // Criptografando "12345" para bater com o BCrypt do AuthController
            string senhaCriptografada = BCrypt.Net.BCrypt.HashPassword("12345");

            var c1 = new BancoAPI.Api.Models.Cliente
            {
                Nome = "Gabriel Cunha",
                Cpf = "11122233344",
                Email = "gabriel@bancounico.com",
                SenhaHash = senhaCriptografada
            };

            var c2 = new BancoAPI.Api.Models.Cliente
            {
                Nome = "Marcelo Dias",
                Cpf = "55566677788",
                Email = "marcelo@bancounico.com",
                SenhaHash = senhaCriptografada
            };

            context.Clientes.AddRange(c1, c2);
            context.SaveChanges();

            // C. Criar Contas Milionárias
            context.Contas.AddRange(
                new BancoAPI.Api.Models.Conta { ClienteId = c1.Id, AgenciaId = idAgencia, NumeroConta = "1001-X", Saldo = 1000000m, TipoConta = "Corrente", StatusConta = "Ativa", DataAbertura = DateTime.Now },
                new BancoAPI.Api.Models.Conta { ClienteId = c2.Id, AgenciaId = idAgencia, NumeroConta = "1002-X", Saldo = 1000000m, TipoConta = "Corrente", StatusConta = "Ativa", DataAbertura = DateTime.Now }
            );
            context.SaveChanges();
            Console.WriteLine(">>> SUCESSO: Banco criado, Gabriel e Marcelo estão milionários e prontos para login!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> ERRO CRÍTICO NA INICIALIZAÇÃO: {ex.Message}");
    }
}

// ============================================================
// 5. CONFIGURAÇÃO DO PIPELINE DE REQUISIÇÕES
// ============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
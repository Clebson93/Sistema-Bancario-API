using BancoAPI.Api.Data;
using BancoAPI.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

namespace BancoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly BancoDigitalDbContext _context;

        public AuthController(IConfiguration config, BancoDigitalDbContext context)
        {
            _config = config;
            _context = context;
        }

        public class LoginModel
        {
            [JsonPropertyName("username")] public string? Username { get; set; }
            [JsonPropertyName("cpf")] public string? Cpf { get; set; }
            [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
        }

        public class RegisterModel
        {
            [JsonPropertyName("nome")] public required string Nome { get; set; }
            [JsonPropertyName("cpf")] public required string Cpf { get; set; }
            [JsonPropertyName("email")] public required string Email { get; set; }
            [JsonPropertyName("senha")] public required string Senha { get; set; }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest("Senha é obrigatória");

            string role = "";
            string sub = "";
            string? clienteId = null;

            // 1. LOGIN ADMIN
            if (!string.IsNullOrWhiteSpace(model.Username))
            {
                var adminUser = _config["Auth:DefaultUser:Username"] ?? "admin";
                var adminPass = _config["Auth:DefaultUser:Password"] ?? "admin123";

                if (model.Username == adminUser && model.Password == adminPass)
                {
                    role = "Admin";
                    sub = adminUser;
                }
                else return Unauthorized("Credenciais de admin inválidas");
            }
            // 2. LOGIN CLIENTE
            else if (!string.IsNullOrWhiteSpace(model.Cpf))
            {
                var cpfLimpo = new string(model.Cpf.Where(char.IsDigit).ToArray());
                var cliente = _context.Clientes.FirstOrDefault(c => c.Cpf == cpfLimpo);

                if (cliente == null)
                    return Unauthorized("Cliente não encontrado");

                // 🔥 CORREÇÃO PARA O ERRO DA IMAGEM image_70d5c2.png
                bool senhaValida = false;
                try
                {
                    // Tenta validar via BCrypt
                    senhaValida = BCrypt.Net.BCrypt.Verify(model.Password, cliente.SenhaHash);
                }
                catch (Exception)
                {
                    // Se o salt for inválido (texto puro no banco), compara diretamente
                    senhaValida = (model.Password == cliente.SenhaHash);
                }

                if (!senhaValida)
                    return Unauthorized("Senha inválida");

                role = "Cliente";
                sub = cliente.Cpf;
                clienteId = cliente.Id.ToString();
            }
            else return BadRequest("Username ou CPF é obrigatório");

            return GerarRespostaToken(sub, role, clienteId);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            var cpfLimpo = new string(model.Cpf.Where(char.IsDigit).ToArray());

            if (_context.Clientes.Any(c => c.Cpf == cpfLimpo)) return Conflict("CPF já cadastrado");
            if (_context.Clientes.Any(c => c.Email == model.Email)) return Conflict("E-mail já cadastrado");

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var cliente = new Cliente
                {
                    Nome = model.Nome,
                    Cpf = cpfLimpo,
                    Email = model.Email,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha)
                };

                _context.Clientes.Add(cliente);
                _context.SaveChanges();

                var agenciaId = _context.Agencias.Select(a => a.Id).FirstOrDefault();
                if (agenciaId == 0) throw new Exception("Nenhuma agência disponível.");

                var count = _context.Contas.Count() + 1;
                var conta = new Conta
                {
                    ClienteId = cliente.Id,
                    AgenciaId = agenciaId,
                    NumeroConta = $"0001-{count:D4}",
                    TipoConta = "Corrente",
                    Saldo = 0,
                    StatusConta = "Ativa",
                    DataAbertura = DateTime.Now
                };

                _context.Contas.Add(conta);
                _context.SaveChanges();

                transaction.Commit();
                return Ok(new { message = "Cadastro realizado!", clienteId = cliente.Id });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Erro: {ex.Message}");
            }
        }

        private IActionResult GerarRespostaToken(string sub, string role, string? clienteId)
        {
            var jwtKey = _config["Jwt:Key"] ?? "SistemaBancarioChaveSecretaForte2024!@#$%XYZ";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, sub),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (clienteId != null) claims.Add(new Claim("ClienteId", clienteId));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"] ?? "BancoAPI",
                audience: _config["Jwt:Audience"] ?? "BancoAPIUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(4),
                signingCredentials: creds);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = role,
                clienteId = clienteId
            });
        }
    }
}
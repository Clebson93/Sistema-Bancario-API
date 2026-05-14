using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BancoAPI.Api.Models;
using BancoAPI.Api.Data;
using Microsoft.AspNetCore.Authorization;

namespace BancoAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientesController : ControllerBase
    {
        private readonly BancoDigitalDbContext _context;

        public ClientesController(BancoDigitalDbContext context)
        {
            _context = context;
        }

        public class ClienteDto
        {
            public int Id { get; set; }
            public string? Nome { get; set; }
            public string? Cpf { get; set; }
            public string? Email { get; set; }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Clientes
                .AsNoTracking()
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Cpf = c.Cpf,
                    Email = c.Email
                })
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ClienteDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Cpf = c.Cpf,
                    Email = c.Email
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpGet("meu-perfil")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> GetMeuPerfil()
        {
            var clienteIdClaim = User.FindFirst("ClienteId")?.Value;
            if (string.IsNullOrEmpty(clienteIdClaim) || !int.TryParse(clienteIdClaim, out int clienteId))
                return Unauthorized();

            var cliente = await _context.Clientes
                .Include(c => c.Contas)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null) return NotFound();

            return Ok(new
            {
                cliente.Id,
                cliente.Nome,
                cliente.Cpf,
                cliente.Email,
                Contas = cliente.Contas?.Select(acc => new { acc.Id, acc.NumeroConta, acc.Saldo, acc.TipoConta, acc.StatusConta })
            });
        }

        [HttpGet("buscar-por-cpf/{cpf}")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> BuscarPorCpf(string cpf)
        {
            var cpfLimpo = new string(cpf.Where(char.IsDigit).ToArray());
            var cliente = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.Cpf == cpfLimpo)
                .Select(c => new { c.Nome, c.Cpf })
                .FirstOrDefaultAsync();

            if (cliente == null) return NotFound(new { message = "CPF não encontrado" });
            return Ok(cliente);
        }

        public class ClienteCreateDto
        {
            public string? Nome { get; set; }
            public string? Cpf { get; set; }
            public string? Email { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Cpf))
                return BadRequest("Nome e CPF são obrigatórios");

            var cpfDigits = new string(dto.Cpf.Where(char.IsDigit).ToArray());
            if (cpfDigits.Length != 11) return BadRequest("CPF inválido. Informe 11 dígitos.");

            var cliente = new Cliente
            {
                Nome = dto.Nome.Trim(),
                Cpf = cpfDigits,
                Email = dto.Email?.Trim() ?? string.Empty,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword("123456")
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = cliente.Id }, new ClienteDto { Id = cliente.Id, Nome = cliente.Nome, Cpf = cliente.Cpf, Email = cliente.Email });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, Cliente model)
        {
            var existing = await _context.Clientes.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Nome = model.Nome;
            existing.Cpf = new string(model.Cpf.Where(char.IsDigit).ToArray());
            existing.Email = model.Email;
            existing.DataNascimento = model.DataNascimento;
            existing.Telefone = model.Telefone;
            existing.Cep = model.Cep;
            existing.Logradouro = model.Logradouro;
            existing.Numero = model.Numero;
            existing.Bairro = model.Bairro;

            if (!string.IsNullOrEmpty(model.SenhaHash) && !model.SenhaHash.StartsWith("$2"))
            {
                existing.SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.SenhaHash);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.Clientes.FindAsync(id);
            if (existing == null) return NotFound();
            _context.Clientes.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
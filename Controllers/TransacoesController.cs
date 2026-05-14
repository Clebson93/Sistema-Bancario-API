using BancoAPI.Api.Data;
using BancoAPI.Api.Models;
using BancoAPI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancoAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransacoesController : ControllerBase
    {
        private readonly BancoDigitalDbContext _context;
        private readonly ContaService _contaService;

        public TransacoesController(BancoDigitalDbContext context, ContaService contaService)
        {
            _context = context;
            _contaService = contaService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Transacao>>> GetTransacoes()
        {
            return await _context.Transacoes
                .Include(t => t.ContaOrigem)
                .Include(t => t.ContaDestino)
                .OrderByDescending(t => t.DataHora)
                .ToListAsync();
        }

        [HttpGet("minhas")]
        [Authorize(Roles = "Cliente")]
        public async Task<ActionResult<IEnumerable<Transacao>>> GetMinhasTransacoes()
        {
            var clienteIdClaim = User.FindFirst("ClienteId")?.Value;
            if (string.IsNullOrEmpty(clienteIdClaim) || !int.TryParse(clienteIdClaim, out int clienteId))
                return Unauthorized();

            var contasDoCliente = await _context.Contas
                .Where(c => c.ClienteId == clienteId)
                .Select(c => c.Id)
                .ToListAsync();

            return await _context.Transacoes
                .Include(t => t.ContaOrigem)
                .Include(t => t.ContaDestino)
                .Where(t => contasDoCliente.Contains(t.ContaOrigemId) || (t.ContaDestinoId != null && contasDoCliente.Contains(t.ContaDestinoId.Value)))
                .OrderByDescending(t => t.DataHora)
                .ToListAsync();
        }

        [HttpPost("depositar")]
        [Authorize]
        public async Task<IActionResult> Depositar([FromBody] DepositoRequest request)
        {
            var sucesso = await _contaService.Depositar(request.ContaId, request.Valor);
            if (!sucesso) return BadRequest(new { message = "Erro ao realizar depósito. Verifique se a conta existe e está ativa." });
            return Ok(new { message = "Depósito realizado com sucesso" });
        }

        [HttpPost("sacar")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Sacar([FromBody] SaqueRequest request)
        {
            var clienteIdClaim = User.FindFirst("ClienteId")?.Value;
            if (string.IsNullOrEmpty(clienteIdClaim) || !int.TryParse(clienteIdClaim, out int clienteId))
                return Unauthorized();

            var conta = await _context.Contas.FindAsync(request.ContaId);
            if (conta == null || conta.ClienteId != clienteId) return Forbid();

            var erro = await _contaService.Sacar(request.ContaId, request.Valor);
            if (erro != null) return BadRequest(new { message = erro });
            return Ok(new { message = "Saque realizado com sucesso" });
        }

        [HttpPost("transferir")]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Transferir([FromBody] TransferenciaRequest request)
        {
            var clienteIdClaim = User.FindFirst("ClienteId")?.Value;
            if (string.IsNullOrEmpty(clienteIdClaim) || !int.TryParse(clienteIdClaim, out int clienteId))
                return Unauthorized();

            var conta = await _context.Contas.FindAsync(request.ContaOrigemId);
            if (conta == null || conta.ClienteId != clienteId) return Forbid();

            var erro = await _contaService.Transferir(request.ContaOrigemId, request.CpfDestino, request.Valor);
            if (erro != null) return BadRequest(new { message = erro });
            return Ok(new { message = "Transferência realizada com sucesso" });
        }

        public class DepositoRequest
        {
            public int ContaId { get; set; }
            public decimal Valor { get; set; }
        }

        public class SaqueRequest
        {
            public int ContaId { get; set; }
            public decimal Valor { get; set; }
        }

        public class TransferenciaRequest
        {
            public int ContaOrigemId { get; set; }
            public string CpfDestino { get; set; } = string.Empty;
            public decimal Valor { get; set; }
        }
    }
}
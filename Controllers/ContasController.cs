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
    public class ContasController : ControllerBase
    {
        private readonly BancoDigitalDbContext _context;

        public ContasController(BancoDigitalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var contas = await _context.Contas
                .Include(c => c.Cliente)
                .Include(c => c.Agencia)
                .AsNoTracking()
                .ToListAsync();
            return Ok(contas);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _context.Contas
                .Include(c => c.Cliente)
                .Include(c => c.Agencia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (item == null) return NotFound();

            if (User.IsInRole("Cliente"))
            {
                var clienteIdClaim = User.FindFirst("ClienteId")?.Value;
                if (item.ClienteId.ToString() != clienteIdClaim)
                    return Forbid();
            }

            return Ok(item);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Conta model)
        {
            if (string.IsNullOrEmpty(model.NumeroConta))
            {
                var count = _context.Contas.Count() + 1;
                model.NumeroConta = $"0001-{count:D4}";
            }

            if (model.DataAbertura == default)
                model.DataAbertura = DateTime.Now;

            _context.Contas.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Conta model)
        {
            if (id != model.Id) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Contas.FindAsync(id);
            if (item == null) return NotFound();
            _context.Contas.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

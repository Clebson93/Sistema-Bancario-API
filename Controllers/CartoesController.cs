using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BancoAPI.Api.Models;
using BancoAPI.Api.Data;

namespace BancoAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartoesController : ControllerBase
    {
        private readonly BancoDigitalDbContext _context;
        public CartoesController(BancoDigitalDbContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.Cartoes.Include(c => c.Conta).AsNoTracking().ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Cartao model)
        {
            _context.Cartoes.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Cartoes.FindAsync(id);
            if (item == null) return NotFound();
            _context.Cartoes.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

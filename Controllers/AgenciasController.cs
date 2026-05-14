using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BancoAPI.Api.Models;
using BancoAPI.Api.Data;

namespace BancoAPI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgenciasController : ControllerBase
    {
        private readonly BancoDigitalDbContext _context;
        public AgenciasController(BancoDigitalDbContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _context.Agencias.AsNoTracking().ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Agencia model)
        {
            _context.Agencias.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }
    }
}

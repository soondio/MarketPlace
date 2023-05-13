using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplicationLab2.Models1;

namespace WebApplicationLab2.Controllers
{
    [Route("api/[controller]")]
    [EnableCors]
    [ApiController]
    public class ComputerController : ControllerBase
    {
        private readonly CompClubWebContext _context;
        public ComputerController(CompClubWebContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComputerDto>>> GetComputers()
        {
            var computers = await _context.Computers
               .Include(c => c.Processor)
               .Include(c => c.VideoCard)
               .Include(c => c.Ram)
               .Include(c => c.Monitor)
               .Select(c => new ComputerDto
               {
                   Id = c.Id,
                   Name = c.Name,
                   ProcessorName = c.Processor.Name,
                   VideocardName = c.VideoCard.Name,
                   MemoryName = c.Ram.Name,
                   MonitorName = c.Monitor.Name
               })
               .ToListAsync();

            return computers;
        }

        [HttpGet("{id}")]
        public IActionResult GetComputerById(int id)
        {
            var computer = _context.Computers.Include(c => c.Processor).Include(c => c.VideoCard).Include(c => c.Ram).Include(c => c.Monitor).FirstOrDefault(c => c.Id == id);

            if (computer == null)
            {
                return NotFound();
            }

            var computerDto = new ComputerDto
            {
                Id = computer.Id,
                Name = computer.Name,
                ProcessorName = computer.Processor.Name,
                VideocardName = computer.VideoCard.Name,
                MemoryName = computer.Ram.Name,
                MonitorName = computer.Monitor.Name
            };

            return Ok(computerDto);
        }

        [HttpGet("/api/availability")]
        public async Task<ActionResult<IEnumerable<Computer>>> GetAvailability(DateTime date, DateTime date2)
        {
            var orders = await _context.Orders
                .Where(o => o.Date >= date && o.EndDate <= date2)
                .ToListAsync();

            var occupiedComputerIds = orders.Select(o => o.ComputerId).Distinct();

            var computers = await _context.Computers
                .Where(c => !occupiedComputerIds.Contains(c.Id))
                .ToListAsync();

            return computers;
        }

    }
}

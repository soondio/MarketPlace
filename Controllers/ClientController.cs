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
    public class ClientController : ControllerBase
    {
        private readonly CompClubWebContext _context;
        private readonly UserManager<User> _usermanager;
        public ClientController(CompClubWebContext context, UserManager<User> userManager)
        {
            _context = context;
            _usermanager= userManager;

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            return Ok(client);
        }
        [HttpGet]
        public async Task<ActionResult<Client>> GetAuthClient()
        {
            var clients = await _context.Clients.ToListAsync();
            User user = await GetCurrentUserAsync();
            if (user != null)
            {
                Client client = clients.FirstOrDefault(c => c.Email == user.Email);
                return Ok(client);
            }
            else
            {
                return Unauthorized(new { message = "Сначала выполните вход" });
            }
        }
        private Task<User> GetCurrentUserAsync() => _usermanager.GetUserAsync(HttpContext.User);
    }
}

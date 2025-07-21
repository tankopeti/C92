using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContactsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetContacts([FromQuery] string search = "", int skip = 0, int take = 50)
        {
            var contacts = await _context.Contacts
                .Where(c => string.IsNullOrEmpty(search) ||
                            c.FirstName.Contains(search) ||
                            c.LastName.Contains(search) ||
                            (c.Email != null && c.Email.Contains(search)))
                .Skip(skip)
                .Take(take)
                .Select(c => new
                {
                    id = c.ContactId,
                    text = $"{c.FirstName} {c.LastName}" + (c.Email != null ? $" ({c.Email})" : "")
                })
                .ToListAsync();

            return Ok(contacts);
        }
    }
}
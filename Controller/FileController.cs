using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Data;
using Cloud9_2.Models;

namespace Cloud9_2.Controllers
{

    [Authorize]
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("file/{documentId}")]
        public async Task<IActionResult> GetFile(int documentId)
        {
            var user = await _userManager.GetUserAsync(User);
            var isAdmin = user != null && (await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "SuperAdmin"));

            var document = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);

            if (document == null || (!isAdmin && document.UploadedBy != User.Identity.Name))
            {
                return Forbid();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var mimeType = "application/pdf"; // Adjust based on file type
            return PhysicalFile(filePath, mimeType, document.FileName);
        }
    }
}
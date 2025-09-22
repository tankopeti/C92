using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    [Authorize]
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        return Ok(new { Status = "Valid" });
    }
}
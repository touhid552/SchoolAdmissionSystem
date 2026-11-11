using Microsoft.AspNetCore.Mvc;

namespace SchoolAdmission.Api.Controllers;

// GET /health answers "healthy" while the application is running.
// It does not check the database.
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy" });
    }
}

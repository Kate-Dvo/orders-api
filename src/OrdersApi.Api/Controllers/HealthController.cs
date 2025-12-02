using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("health")]
[DisableRateLimiting]
public class HealthController : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Liveness() => Ok("healthy");

    [HttpGet("ready")]
    public IActionResult Ready() => Ok("ready");
}
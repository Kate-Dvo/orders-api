using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrdersApi.Api.Helpers;

namespace OrdersApi.Api.Controllers;

[ApiController]
[ApiVersion(Consts.ApiVersion1)]
[Route("health/v{version:apiVersion}/[controller]")]
[DisableRateLimiting]
public class HealthController : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Liveness() => Ok("healthy");

    [HttpGet("ready")]
    public IActionResult Ready() => Ok("ready");
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSensors.Web.Controllers;

/// <summary>
/// Home controller.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class ApiHomeController : ControllerBase
{
    private readonly ILogger _logger;

    /// <summary>
    /// Construct a new controller.
    /// </summary>
    /// <param name="logger"></param>
    public ApiHomeController(ILogger<ApiHomeController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Static error page.
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    [Route("/error")]
    public IActionResult Error()
    {
        _logger.LogInformation("Error page requested.");
        return File("error.html", "text/html");
    }

    /// <summary>
    /// Static forbidden page.
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet]
    [Route("/forbidden")]
    public IActionResult Forbidden()
    {
        _logger.LogInformation("Forbidden page requested.");
        return File("forbidden.html", "text/html");
    }

    /// <summary>
    /// Home page.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult Index()
    {
        _logger.LogInformation("Home page requested.");
        return File("app.html", "text/html");
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[Route("[controller]")]
public class HomeController : Controller
{
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Index()
    {
        return File("~/index.html", "text/html");
    }
}
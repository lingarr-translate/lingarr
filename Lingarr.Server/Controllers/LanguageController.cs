using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Services;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguageController : ControllerBase
{
    private readonly LanguageService _languageService;

    public LanguageController(LanguageService languageService)
    {
        _languageService = languageService ?? throw new ArgumentNullException(nameof(languageService));
    }

    [HttpGet]
    public async Task<IActionResult> index()
    {
        try
        {
            var languageList = await _languageService.GetLanguages();
            return Ok(languageList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}

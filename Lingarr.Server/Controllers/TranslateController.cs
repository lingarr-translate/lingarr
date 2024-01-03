using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Models;
using Lingarr.Server.Services;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly TranslateService _translateService;

    public TranslateController(TranslateService translateService)
    {
        _translateService = translateService ?? throw new ArgumentNullException(nameof(translateService));
    }

    [HttpPost]
    public async Task<IActionResult> Translate([FromBody] TranslateSubtitle translateSubtitle)
    {
        try
        {
            var result = await _translateService.TranslateAsync(translateSubtitle.SubtitlePath, translateSubtitle.TargetLanguage);
            return new JsonResult(result);
        }
        catch (ApplicationException ex)
        {
            return BadRequest("Translation failed. Please check your input and try again.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }
}

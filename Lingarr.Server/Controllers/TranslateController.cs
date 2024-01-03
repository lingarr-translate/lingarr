using Microsoft.AspNetCore.Mvc;
using Lingarr.Server.Models;
using Lingarr.Server.Services;

namespace Lingarr.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TranslateController : ControllerBase
{
    private readonly TranslateService _translateService;

    public TranslateController(DirectoryService directoryService, TranslateService translateService)
    {
        _translateService = translateService ?? throw new ArgumentNullException(nameof(translateService));
    }

    [HttpPost()]
    public async Task<IActionResult> Translate([FromBody] TranslateSubtitle translateSubtitle)
    {
        var result = await _translateService.Translate(translateSubtitle.SubtitlePath, translateSubtitle.TargetLanguage);
        return new JsonResult(result);
    }
}

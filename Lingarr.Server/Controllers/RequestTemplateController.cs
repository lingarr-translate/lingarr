using Lingarr.Server.Attributes;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Controllers;

[ApiController]
[LingarrAuthorize]
[Route("api/[controller]")]
public class RequestTemplateController : ControllerBase
{
    private readonly IRequestTemplateService _requestTemplateService;

    public RequestTemplateController(IRequestTemplateService requestTemplateService)
    {
        _requestTemplateService = requestTemplateService;
    }

    /// <summary>
    /// Retrieves the default request templates for all AI providers.
    /// </summary>
    /// <returns>A dictionary mapping setting keys to their default JSON template strings.</returns>
    [HttpGet("defaults")]
    public ActionResult<Dictionary<string, string>> GetDefaults()
    {
        var defaultTemplates = _requestTemplateService.GetDefaultTemplates();
        return Ok(defaultTemplates);
    }
}

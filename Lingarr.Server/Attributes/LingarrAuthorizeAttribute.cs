using Lingarr.Core.Configuration;
using Lingarr.Server.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lingarr.Server.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LingarrAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if [AllowAnonymous] is present, if so, skip all authorization
        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
        {
            return;
        }

        var settingService = context.HttpContext.RequestServices.GetRequiredService<ISettingService>();
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

        // Check if onboarding is completed
        var onboardingCompleted = await settingService.GetSetting(SettingKeys.Authentication.OnboardingCompleted);
        if (string.IsNullOrEmpty(onboardingCompleted) || onboardingCompleted != "true")
        {
            context.Result = new ObjectResult(new
            {
                message = "Onboarding required",
                onboardingRequired = true
            })
            {
                StatusCode = 403
            };
            return;
        }

        // Check if authentication is enabled or disabled
        var authEnabled = await settingService.GetSetting(SettingKeys.Authentication.AuthEnabled);
        if (authEnabled == "false")
        {
            return;
        }

        // Validate authentication (cookie or API key)
        var isCookieAuthenticated = context.HttpContext.User.Identity?.IsAuthenticated ?? false;
        var isApiKeyValid = await ValidateApiKey(context, authService);

        if (!isCookieAuthenticated && !isApiKeyValid)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Authentication required",
                authenticated = false
            });
        }
    }

    private static async Task<bool> ValidateApiKey(AuthorizationFilterContext context, IAuthService authService)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeaderValues))
            return false;

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        return !string.IsNullOrWhiteSpace(providedApiKey) && await authService.ValidateApiKey(providedApiKey);
    }
}

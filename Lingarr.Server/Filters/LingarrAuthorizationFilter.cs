using Hangfire.Dashboard;

namespace Lingarr.Server.Filters;

public class LingarrAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return true;
    }
}
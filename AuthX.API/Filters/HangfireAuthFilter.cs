using Hangfire.Dashboard;

namespace AuthX.API.Filters;

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        // Only authenticated admins can see Hangfire dashboard
        return http.User.Identity?.IsAuthenticated == true &&
               http.User.IsInRole("Admin");
    }
}
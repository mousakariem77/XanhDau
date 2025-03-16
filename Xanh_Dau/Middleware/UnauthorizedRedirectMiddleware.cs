public class UnauthorizedRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public UnauthorizedRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == 401) context.Response.Redirect("/Auth/Login");
    }
}
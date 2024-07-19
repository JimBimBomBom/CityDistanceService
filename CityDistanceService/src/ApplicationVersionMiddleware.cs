// Adds a header to the response with the application version
class ApplicationVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _appVersion;

    public ApplicationVersionMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _appVersion = configuration["APP_VERSION"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() => {
            context.Response.Headers["Application-Version"] = _appVersion;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
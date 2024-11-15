namespace VideoPlayer.Api;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(5000, listenOptions =>
//     {
//         listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
//     });
// });

        // Configure services
        ConfigureServices(builder.Services);

        var app = builder.Build();

        // Configure middleware and HTTP request pipeline
        ConfigureMiddleware(app);

        // Start the application
        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register services in the DI container
        services.AddControllers(); // Add controllers for API endpoints
        services.AddEndpointsApiExplorer(); // Enable OpenAPI endpoint exploration
        services.AddSwaggerGen(); // Add Swagger for API documentation
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        // Enable Swagger in development
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Enable serving static files (e.g., wwwroot folder)
        app.UseStaticFiles();

        // Enable routing
        app.UseRouting();

        // Enable authorization middleware
        app.UseAuthorization();

        // Configure route mappings
        app.UseEndpoints(endpoints =>
        {
            // Map controller endpoints
            endpoints.MapControllers();
        });
    }
}
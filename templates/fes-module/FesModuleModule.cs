using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FesStarter.FesModule;

public static class FesModuleModule
{
    public static IServiceCollection AddFesModule(this IServiceCollection services)
    {
        // Register module-specific services here
        return services;
    }

    public static IEndpointRouteBuilder MapFesModuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fesmodule")
            .WithTags("FesModule");

        // Map endpoints here
        // Example: group.MapPost("/", Features.CreateSomething.Endpoint);

        return app;
    }
}

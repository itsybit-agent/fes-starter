using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FesStarter.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddSingleton<StockReadModel>();
        services.AddScoped<InitializeStockHandler>();
        services.AddScoped<GetStockHandler>();
        return services;
    }

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        InitializeStockEndpoint.Map(app);
        GetStockEndpoint.Map(app);
        return app;
    }
}

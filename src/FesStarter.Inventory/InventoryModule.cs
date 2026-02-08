using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FesStarter.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddSingleton<StockReadModel>();
        services.AddSingleton<StockReadModelProjections>();
        services.AddScoped<InitializeStockHandler>();
        services.AddScoped<GetStockHandler>();
        services.AddScoped<ReserveStockOnOrderPlacedHandler>();
        services.AddScoped<DeductStockOnOrderShippedHandler>();
        return services;
    }

    public static WebApplication MapInventoryEndpoints(this WebApplication app)
    {
        InitializeStockEndpoint.Map(app);
        GetStockEndpoint.Map(app);
        return app;
    }
}

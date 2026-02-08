using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FesStarter.Orders;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddSingleton<OrderReadModel>();
        services.AddScoped<PlaceOrderHandler>();
        services.AddScoped<ShipOrderHandler>();
        services.AddScoped<ListOrdersHandler>();
        return services;
    }

    public static WebApplication MapOrderEndpoints(this WebApplication app)
    {
        PlaceOrderEndpoint.Map(app);
        ShipOrderEndpoint.Map(app);
        ListOrdersEndpoint.Map(app);
        return app;
    }
}

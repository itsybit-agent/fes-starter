using FileEventStore;
using FesStarter.Orders;
using FesStarter.Inventory;

namespace FesStarter.Api.Infrastructure;

public class ReadModelInitializer(
    OrderReadModel orderReadModel,
    StockReadModel stockReadModel,
    IEventStore eventStore,
    IWebHostEnvironment environment,
    ILogger<ReadModelInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var dataPath = Path.Combine(environment.ContentRootPath, "data", "events", "streams");

        if (!Directory.Exists(dataPath))
        {
            logger.LogInformation("No event data found, starting fresh");
            return;
        }

        var streams = Directory.GetDirectories(dataPath);
        logger.LogInformation("Rebuilding read models from {Count} streams", streams.Length);

        foreach (var streamDir in streams)
        {
            var streamId = Path.GetFileName(streamDir);
            try
            {
                var events = await eventStore.FetchEventsAsync(streamId);
                foreach (var evt in events)
                {
                    orderReadModel.Apply(evt);
                    stockReadModel.Apply(evt);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load stream {StreamId}", streamId);
            }
        }

        logger.LogInformation("Read models ready: {Orders} orders, {Products} products",
            orderReadModel.GetAll().Count, stockReadModel.GetAll().Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

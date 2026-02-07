using FileEventStore;
using FesStarter.Api.Features.Inventory;
using FesStarter.Api.Features.Orders;

namespace FesStarter.Api.Infrastructure;

/// <summary>
/// Rebuilds all read models from events on startup.
/// Scans all stream directories and replays events.
/// </summary>
public class ReadModelInitializer : IHostedService
{
    private readonly OrderReadModel _orderReadModel;
    private readonly StockReadModel _stockReadModel;
    private readonly IEventStore _eventStore;
    private readonly string _dataPath;
    private readonly ILogger<ReadModelInitializer> _logger;

    public ReadModelInitializer(
        OrderReadModel orderReadModel,
        StockReadModel stockReadModel,
        IEventStore eventStore,
        IWebHostEnvironment environment,
        ILogger<ReadModelInitializer> logger)
    {
        _orderReadModel = orderReadModel;
        _stockReadModel = stockReadModel;
        _eventStore = eventStore;
        _dataPath = Path.Combine(environment.ContentRootPath, "data", "events");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_dataPath))
        {
            _logger.LogInformation("No event data directory found, starting with empty read models");
            return;
        }

        var streamDirs = Directory.GetDirectories(_dataPath);
        _logger.LogInformation("Rebuilding read models from {Count} streams", streamDirs.Length);

        foreach (var streamDir in streamDirs)
        {
            var streamId = Path.GetFileName(streamDir);
            try
            {
                var events = await _eventStore.FetchEventsAsync(streamId);
                foreach (var evt in events)
                {
                    // Route events to appropriate read models
                    _orderReadModel.Apply(evt);
                    _stockReadModel.Apply(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load stream {StreamId}", streamId);
            }
        }

        _logger.LogInformation(
            "Read models rebuilt: {OrderCount} orders, {StockCount} products", 
            _orderReadModel.GetAll().Count,
            _stockReadModel.GetAll().Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

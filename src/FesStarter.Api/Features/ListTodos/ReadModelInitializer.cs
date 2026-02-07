using FileEventStore;

namespace FesStarter.Api.Features.ListTodos;

/// <summary>
/// Rebuilds the read model from events on startup.
/// Scans all stream directories and replays events.
/// </summary>
public class ReadModelInitializer : IHostedService
{
    private readonly TodoReadModel _readModel;
    private readonly IEventStore _eventStore;
    private readonly string _dataPath;
    private readonly ILogger<ReadModelInitializer> _logger;

    public ReadModelInitializer(
        TodoReadModel readModel, 
        IEventStore eventStore,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<ReadModelInitializer> logger)
    {
        _readModel = readModel;
        _eventStore = eventStore;
        _dataPath = Path.Combine(environment.ContentRootPath, "data", "events");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_dataPath))
        {
            _logger.LogInformation("No event data directory found, starting with empty read model");
            return;
        }

        var streamDirs = Directory.GetDirectories(_dataPath);
        _logger.LogInformation("Rebuilding read model from {Count} streams", streamDirs.Length);

        foreach (var streamDir in streamDirs)
        {
            var streamId = Path.GetFileName(streamDir);
            try
            {
                var events = await _eventStore.FetchEventsAsync(streamId);
                foreach (var evt in events)
                {
                    _readModel.Apply(evt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load stream {StreamId}", streamId);
            }
        }

        _logger.LogInformation("Read model rebuilt with {Count} todos", _readModel.GetAll().Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

namespace FesStarter.Api.Infrastructure;

/// <summary>
/// Scoped service that tracks correlation and causation IDs for distributed tracing.
/// Flows through event-driven systems to link all related operations.
/// </summary>
public class CorrelationContext
{
    private string _correlationId = "";
    private string? _causationId;

    /// <summary>
    /// Gets or sets the correlation ID (request trace ID).
    /// </summary>
    public string CorrelationId
    {
        get => string.IsNullOrEmpty(_correlationId) ? Guid.NewGuid().ToString() : _correlationId;
        set => _correlationId = value;
    }

    /// <summary>
    /// Gets or sets the causation ID (ID of event that triggered this operation).
    /// </summary>
    public string? CausationId
    {
        get => _causationId;
        set => _causationId = value;
    }

    /// <summary>
    /// Initialize with explicit IDs (from HTTP headers or elsewhere).
    /// </summary>
    public void Initialize(string correlationId, string? causationId = null)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}

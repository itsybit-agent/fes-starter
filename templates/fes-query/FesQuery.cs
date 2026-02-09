using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FesStarter.FesModule.Features;

// Query
public record FesQuery() : IRequest<IEnumerable<FesQueryItem>>;

public record FesQueryItem(string Id, string Name);

// Read Model
public static class FesQueryReadModel
{
    private static readonly List<FesQueryItem> _items = new();

    public static IEnumerable<FesQueryItem> GetAll() => _items.AsReadOnly();

    public static void Add(FesQueryItem item) => _items.Add(item);

    public static void Clear() => _items.Clear();
}

// Handler
public class FesQueryHandler : IRequestHandler<FesQuery, IEnumerable<FesQueryItem>>
{
    public Task<IEnumerable<FesQueryItem>> Handle(FesQuery request, CancellationToken ct)
    {
        return Task.FromResult(FesQueryReadModel.GetAll());
    }
}

// Endpoint
public static class FesQueryEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new FesQuery());
            return Results.Ok(result);
        })
        .WithName("FesQuery")
        .WithOpenApi();
    }
}

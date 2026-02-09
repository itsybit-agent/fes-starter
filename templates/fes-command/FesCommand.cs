using FileEventStore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FesStarter.FesModule.Features;

// Command
public record FesCommand(string Id, string Name) : IRequest<FesCommandResult>;

public record FesCommandResult(string Id);

// Handler
public class FesCommandHandler(IEventSession session) : IRequestHandler<FesCommand, FesCommandResult>
{
    public async Task<FesCommandResult> Handle(FesCommand request, CancellationToken ct)
    {
        var aggregate = await session.AggregateStreamOrCreateAsync<Domain.FesModuleAggregate>(
            request.Id, ct);

        // TODO: Call aggregate method
        // aggregate.DoSomething(request.Name);

        await session.SaveChangesAsync(ct);

        return new FesCommandResult(request.Id);
    }
}

// Endpoint
public static class FesCommandEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/", async (FesCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("FesCommand")
        .WithOpenApi();
    }
}

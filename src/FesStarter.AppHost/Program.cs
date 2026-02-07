var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.FesStarter_Api>("api");

// Angular dev server on default port 4200
builder.AddNpmApp("web", "../FesStarter.Web", "start")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(targetPort: 4200)
    .WithExternalHttpEndpoints();

builder.Build().Run();

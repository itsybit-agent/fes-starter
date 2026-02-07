var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.FesStarter_Api>("api");

var web = builder.AddNpmApp("web", "../FesStarter.Web", "start")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FesStarter.Api.Tests.Infrastructure;

/// <summary>
/// Test fixture that creates an isolated test environment with its own event store.
/// Each test run gets a fresh data directory to ensure test isolation.
/// </summary>
public class ApiTestFixture : WebApplicationFactory<Program>
{
    private readonly string _testDataPath;

    public ApiTestFixture()
    {
        // Each fixture instance gets a unique data directory
        _testDataPath = Path.Combine(Path.GetTempPath(), "fes-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataPath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.UseSetting("DataPath", _testDataPath);
        
        builder.ConfigureServices(services =>
        {
            // Additional test-specific service overrides can go here
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        // Clean up test data directory
        if (disposing && Directory.Exists(_testDataPath))
        {
            try
            {
                Directory.Delete(_testDataPath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}

/// <summary>
/// Collection definition for tests that share the same fixture.
/// Use [Collection("Api")] on test classes to share the same server instance.
/// </summary>
[CollectionDefinition("Api")]
public class ApiTestCollection : ICollectionFixture<ApiTestFixture>
{
}

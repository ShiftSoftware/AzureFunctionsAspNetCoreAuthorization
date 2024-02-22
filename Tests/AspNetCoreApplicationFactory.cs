using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Tests;

public class AspNetCoreApplicationFactory : WebApplicationFactory<API.Sample.Marker>
{
    public AspNetCoreApplicationFactory()
    {

    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        var host = builder.Build();

        host.Start();

        return host;
    }
}

[CollectionDefinition("API Collection")]
public class APICollection : ICollectionFixture<AspNetCoreApplicationFactory>
{
}
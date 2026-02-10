using Microsoft.Extensions.DependencyInjection;
using OutlookSync.AI.Extensions;
using OutlookSync.AI.Interfaces;

namespace OutlookSync.AI.Tests.Services;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddOutlookSyncAI_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOutlookSyncAI();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IPrivacyService>());
        Assert.NotNull(provider.GetService<IAiService>());
        Assert.NotNull(provider.GetService<IAgentExecutor>());
    }
}

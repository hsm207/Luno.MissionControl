using NetArchTest.Rules;
using Xunit;

namespace Luno.MissionControl.Architecture.Tests;

public class ClientLayerTests
{
    [Fact(DisplayName = "Web.Client (WASM) must not depend on Infrastructure or Server-side internals.")]
    public void WebClient_Should_Not_Depend_On_Internal_Adapters_Or_Server()
    {
        var result = Types.InAssembly(TestAssemblies.WebClient)
            .ShouldNot()
            .HaveDependencyOnAny(
                TestAssemblies.Infrastructure.GetName().Name!,
                "Luno.MissionControl.Web.Hubs",
                "Luno.MissionControl.Web.Services",
                "Luno.MissionControl.Web.Controllers",
                "Luno.MissionControl.Web.Components"
            )
            .GetResult();

        Assert.True(result.IsSuccessful, $"Web.Client has illegal dependencies: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }
}

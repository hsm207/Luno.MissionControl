using NetArchTest.Rules;
using Xunit;

namespace Luno.MissionControl.Architecture.Tests;

public class LayerDependencyTests
{
    [Fact(DisplayName = "Core must have zero dependencies on any other internal projects.")]
    public void Core_Should_Not_Have_Internal_Dependencies()
    {
        var result = Types.InAssembly(TestAssemblies.Core)
            .ShouldNot()
            .HaveDependencyOnAny(TestAssemblies.GetOtherInternalNames(TestAssemblies.Core))
            .GetResult();

        Assert.True(result.IsSuccessful, $"Core has illegal internal dependencies: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    [Fact(DisplayName = "Application must only depend on Core.")]
    public void Application_Should_Only_Depend_On_Core()
    {
        var result = Types.InAssembly(TestAssemblies.Application)
            .ShouldNot()
            .HaveDependencyOnAny(TestAssemblies.GetOtherInternalNames(TestAssemblies.Application, TestAssemblies.Core))
            .GetResult();

        Assert.True(result.IsSuccessful, $"Application has illegal dependencies: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    [Fact(DisplayName = "Infrastructure must not depend on Web or Web.Client.")]
    public void Infrastructure_Should_Not_Depend_On_Web_Layers()
    {
        var result = Types.InAssembly(TestAssemblies.Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(TestAssemblies.Web.GetName().Name!, TestAssemblies.WebClient.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful, $"Infrastructure has illegal dependencies: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }
}

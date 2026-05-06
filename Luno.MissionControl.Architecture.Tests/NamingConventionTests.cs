using NetArchTest.Rules;
using Xunit;

namespace Luno.MissionControl.Architecture.Tests;

public class NamingConventionTests
{
    [Fact(DisplayName = "Ports in Application layer must exist and be interfaces starting with 'I'.")]
    public void Application_Ports_Should_Exist_And_Be_Interfaces_Starting_With_I()
    {
        var targetTypes = Types.InAssembly(TestAssemblies.Application)
            .That()
            .ResideInNamespace("Luno.MissionControl.Application.Ports");

        Assert.NotEmpty(targetTypes.GetTypes());

        var result = targetTypes
            .And()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Application ports must be 'I' prefixed interfaces: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    [Fact(DisplayName = "Infrastructure Adapters must exist and be named with 'Bridge' or 'Adapter' suffix.")]
    public void Infrastructure_Adapters_Should_Exist_And_Have_Correct_Suffix()
    {
        var targetTypes = Types.InAssembly(TestAssemblies.Infrastructure)
            .That()
            .ResideInNamespace("Luno.MissionControl.Infrastructure.Adapters")
            .And()
            .AreClasses();

        Assert.NotEmpty(targetTypes.GetTypes());

        var result = targetTypes
            .Should()
            .HaveNameEndingWith("Bridge")
            .Or()
            .HaveNameEndingWith("Adapter")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Infrastructure adapters must follow naming conventions: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    [Fact(DisplayName = "Web Controllers must exist and end with 'Controller'.")]
    public void Web_Controllers_Should_Exist_And_Have_Correct_Suffix()
    {
        var targetTypes = Types.InAssembly(TestAssemblies.Web)
            .That()
            .ResideInNamespace("Luno.MissionControl.Web.Controllers")
            .And()
            .AreClasses()
            .And()
            .AreNotNested(); // Excludes compiler-generated classes for async/await/lambdas

        Assert.NotEmpty(targetTypes.GetTypes());

        var result = targetTypes
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Web controllers must end with 'Controller': {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }

    [Fact(DisplayName = "Web.Client ViewModels/Presenters must exist and follow naming conventions.")]
    public void WebClient_Presenters_Should_Exist_And_Have_Correct_Suffix()
    {
        var targetTypes = Types.InAssembly(TestAssemblies.WebClient)
            .That()
            .HaveNameEndingWith("ViewModel")
            .Or()
            .HaveNameEndingWith("Presenter")
            .And()
            .AreClasses();

        Assert.NotEmpty(targetTypes.GetTypes());

        var result = targetTypes
            .Should()
            .ResideInNamespaceContaining("Components")
            .GetResult();

        Assert.True(result.IsSuccessful, $"ViewModels/Presenters must reside in component-related namespaces: {string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? [])}");
    }
}

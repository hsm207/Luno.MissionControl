using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// A generic, lightweight bridge that implements <see cref="IHostEnvironment"/> to satisfy 
/// dependencies in environments that don't natively support the generic host (like Blazor WebAssembly).
/// This satisfies the Dependency Inversion Principle by allowing shared components to depend on 
/// a stable abstraction rather than a framework-specific interface.
/// </summary>
public sealed class HostEnvironmentBridge(string environmentName, string applicationName) : IHostEnvironment
{
    /// <inheritdoc />
    public string EnvironmentName { get; set; } = environmentName;

    /// <inheritdoc />
    public string ApplicationName { get; set; } = applicationName;

    /// <inheritdoc />
    public string ContentRootPath { get; set; } = string.Empty;

    /// <inheritdoc />
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
}

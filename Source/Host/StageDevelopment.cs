// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;

namespace Cratis.Stage.Host;

/// <summary>
/// What Stage means by development. Stage owns the definition and tells ASP.NET Core and Arc about it, instead of
/// inheriting it from <c language="shell">ASPNETCORE_ENVIRONMENT</c>, except that a Production host environment is not development.
/// </summary>
/// <remarks>
/// <para>
/// The play sandbox is development unless its host environment is Production or
/// <c language="shell">Stage:Development</c> is <c language="csharp">false</c>. An explicit true setting overrides the environment.
/// A published application is never development, and no setting makes it so.
/// </para>
/// <para>
/// Development is what makes the surfaces meant for tools open: Arc maps its user, tenant and identity discovery endpoints
/// anonymously, and the sandbox may offer its modeled users to Lens. It is not what turns any of them on. Those stay explicit
/// settings, such as <c language="shell">Stage:Sandbox:Users</c>.
/// </para>
/// </remarks>
internal static class StageDevelopment
{
    internal const string Key = "Stage:Development";

    /// <summary>
    /// Decides whether this run is development. An explicit Stage setting wins over the host environment;
    /// otherwise a sandbox is development unless the host environment is Production.
    /// </summary>
    /// <param name="sandbox">Whether Stage runs as the play sandbox, as opposed to a published application.</param>
    /// <param name="configuration">The configuration the host runs with.</param>
    /// <param name="environment">The host environment the process started with.</param>
    /// <returns><see langword="true"/> when Stage is in development.</returns>
    internal static bool Decide(bool sandbox, IConfiguration configuration, IHostEnvironment environment) => sandbox && (configuration.GetValue<bool?>(Key)
        ?? !environment.IsProduction());

    /// <summary>
    /// Works out the environment name ASP.NET Core and Arc must see for what Stage decided.
    /// </summary>
    /// <param name="development">What <see cref="Decide"/> returned.</param>
    /// <param name="current">The environment name the process was given, if any.</param>
    /// <returns>Development when Stage is in development, Production when it is not but was handed Development, and otherwise <see langword="null"/>, to leave it alone.</returns>
    internal static string? EnvironmentFor(bool development, string? current)
    {
        if (development)
        {
            return Environments.Development;
        }

        return string.Equals(current, Environments.Development, StringComparison.OrdinalIgnoreCase) ? Environments.Production : null;
    }

    /// <summary>
    /// Tells ASP.NET Core, and through it Arc, what Stage decided, by answering for the host environment with Stage's own definition.
    /// </summary>
    /// <remarks>
    /// The environment name the process started with keeps choosing which configuration files load: the Docker image's
    /// <c language="shell">appsettings.Docker.json</c> still applies. Only what the services answer to "is this development" changes,
    /// and that is what Arc's discovery endpoints, query health and the rest of its development-only behavior consult.
    /// </remarks>
    /// <param name="services">The services of the host being built.</param>
    /// <param name="environment">The host environment the process started with.</param>
    /// <param name="development">What <see cref="Decide"/> returned.</param>
    internal static void Inform(IServiceCollection services, IWebHostEnvironment environment, bool development)
    {
        var name = EnvironmentFor(development, environment.EnvironmentName);
        if (name is null)
        {
            return;
        }

        var informed = new StageHostEnvironment(environment, name);
        services.Replace(ServiceDescriptor.Singleton<IHostEnvironment>(informed));
        services.Replace(ServiceDescriptor.Singleton<IWebHostEnvironment>(informed));
    }
}

/// <summary>
/// The host environment as Stage defines it: the environment the process started with, answering to another name.
/// </summary>
/// <param name="inner">The environment the process started with.</param>
/// <param name="environmentName">The name Stage answers with.</param>
internal sealed class StageHostEnvironment(IWebHostEnvironment inner, string environmentName) : IWebHostEnvironment
{
    /// <inheritdoc/>
    public string EnvironmentName { get; set; } = environmentName;

    /// <inheritdoc/>
    public string ApplicationName
    {
        get => inner.ApplicationName;
        set => inner.ApplicationName = value;
    }

    /// <inheritdoc/>
    public string WebRootPath
    {
        get => inner.WebRootPath;
        set => inner.WebRootPath = value;
    }

    /// <inheritdoc/>
    public IFileProvider WebRootFileProvider
    {
        get => inner.WebRootFileProvider;
        set => inner.WebRootFileProvider = value;
    }

    /// <inheritdoc/>
    public string ContentRootPath
    {
        get => inner.ContentRootPath;
        set => inner.ContentRootPath = value;
    }

    /// <inheritdoc/>
    public IFileProvider ContentRootFileProvider
    {
        get => inner.ContentRootFileProvider;
        set => inner.ContentRootFileProvider = value;
    }
}

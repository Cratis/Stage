// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Publishing;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Adds a capability, such as the Notes backend, to the published host. Extensions may not map tooling routes.
/// </summary>
internal interface IPublishedHostExtension
{
    /// <summary>
    /// Gets the name of the extension. <see cref="PublishedHostExtensions.Notes"/> is only used when Notes is enabled.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers services before the application is built.
    /// </summary>
    /// <param name="context">The host context.</param>
    void ConfigureServices(PublishedHostContext context);

    /// <summary>
    /// Maps endpoints after the application is built.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <param name="context">The host context.</param>
    void MapEndpoints(WebApplication app, PublishedHostContext context);
}

/// <summary>
/// What an extension of the published host can see.
/// </summary>
/// <param name="Builder">The web application builder, to register services on the Arc application and Chronicle event store.</param>
/// <param name="Options">The deployment configuration.</param>
/// <param name="Manifest">The manifest of the package being served.</param>
/// <param name="EventStore">The Chronicle event store name.</param>
internal sealed record PublishedHostContext(WebApplicationBuilder Builder, PublishedOptions Options, PackageManifest Manifest, string EventStore);

/// <summary>
/// The extension point of the published host. The Notes backend registers an extension named <see cref="Notes"/>
/// here and is composed in only when <c language="shell">Stage:Published:Notes:Enabled</c> (or the manifest) asks for it.
/// </summary>
internal static class PublishedHostExtensions
{
    internal const string Notes = "Notes";

    static readonly List<IPublishedHostExtension> _registered = [new NotesPublishedExtension()];

    internal static IReadOnlyList<IPublishedHostExtension> Registered => _registered;

    internal static void Register(IPublishedHostExtension extension) => _registered.Add(extension);

    internal static bool NotesEnabled(PublishedOptions options, PackageManifest manifest) => options.NotesEnabled ?? manifest.NotesEnabled;

    /// <summary>
    /// Selects the extensions to compose, or says why the request cannot be met.
    /// </summary>
    /// <param name="registered">Every registered extension.</param>
    /// <param name="notesEnabled">Whether Notes is enabled.</param>
    /// <returns>The extensions to compose.</returns>
    /// <exception cref="PublishedModeRefused">Notes is enabled but no Notes backend is registered.</exception>
    internal static IReadOnlyList<IPublishedHostExtension> Select(IEnumerable<IPublishedHostExtension> registered, bool notesEnabled)
    {
        var all = registered.ToArray();
        if (notesEnabled && !all.Any(extension => extension.Name == Notes))
        {
            throw new PublishedModeRefused(["Notes is enabled but this Stage build has no Notes backend registered."]);
        }

        return [.. all.Where(extension => extension.Name != Notes || notesEnabled)];
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Showrunner.Notes;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Hosts the Showrunner Notes backend in the published host, configured from the <c language="shell">Notes</c> section.
/// </summary>
internal sealed class NotesPublishedExtension : IPublishedHostExtension
{
    internal const string Section = "Notes";

    /// <inheritdoc/>
    public string Name => PublishedHostExtensions.Notes;

    /// <inheritdoc/>
    public void ConfigureServices(PublishedHostContext context)
    {
        var problems = Problems(Bind(context.Builder.Configuration));
        if (problems.Count > 0)
        {
            throw new PublishedModeRefused(problems);
        }

        // Registered before Notes' own workers so the event types are known to the store by the time they need them.
        context.Builder.Services.AddHostedService<NotesChronicleRegistration>();
        context.Builder.AddNotes(notes => Bind(context.Builder.Configuration, notes));
    }

    /// <inheritdoc/>
    public void MapEndpoints(WebApplication app, PublishedHostContext context)
    {
        // Notes' commands and queries are served by Arc's discovery of the referenced assembly. Its events, reactors and read models
        // are registered with Chronicle by NotesChronicleRegistration, not by the client's own discovery, which Stage turns off.
        // The collecting endpoints belong to the receiving side and are not mapped here.
    }

    /// <summary>
    /// Reads the options from the <c language="shell">Notes</c> configuration section. The configuration binder cannot construct the
    /// strongly typed values of <see cref="NotesOptions"/>, so those are read from their text.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="options">The options to fill, or <see langword="null"/> for new ones.</param>
    /// <returns>The options.</returns>
    internal static NotesOptions Bind(IConfiguration configuration, NotesOptions? options = null)
    {
        options ??= new NotesOptions();
        var section = configuration.GetSection(Section);
        section.Bind(options);
        if (section["ApplicationName"] is { Length: > 0 } applicationName) options.ApplicationName = applicationName;
        if (section["PublicationId"] is { Length: > 0 } publicationId) options.PublicationId = publicationId;
        if (section["ApplicationVersion"] is { Length: > 0 } applicationVersion) options.ApplicationVersion = applicationVersion;
        if (section["CollectorTenantId"] is { Length: > 0 } tenant) options.CollectorTenantId = tenant;
        if (section.GetSection("CollectingStores").Get<string[]>() is { Length: > 0 } stores) options.CollectingStores = [.. stores.Select(store => (EventStoreName)store)];
        return options;
    }

    /// <summary>
    /// Lists everything that stops Notes from running with the given options.
    /// </summary>
    /// <param name="options">The bound options.</param>
    /// <returns>The problems, empty when the configuration is complete.</returns>
    internal static IReadOnlyList<string> Problems(NotesOptions options)
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(options.ApplicationName.Value))
        {
            problems.Add($"Notes is enabled but {Section}:ApplicationName is not set.");
        }

        if (string.IsNullOrWhiteSpace(options.PublicationId.Value))
        {
            problems.Add($"Notes is enabled but {Section}:PublicationId is not set.");
        }

        if (options.Transport == NotesTransport.Http)
        {
            if (!Uri.TryCreate(options.CollectorUrl, UriKind.Absolute, out var url) || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
            {
                problems.Add($"Notes uses the Http transport but {Section}:CollectorUrl is not an absolute http(s) URL.");
            }

            if (string.IsNullOrWhiteSpace(options.CollectorToken) && string.IsNullOrWhiteSpace(options.CollectorTokenFile))
            {
                problems.Add($"Notes uses the Http transport but neither {Section}:CollectorTokenFile nor {Section}:CollectorToken is set.");
            }
            else if (!string.IsNullOrWhiteSpace(options.CollectorTokenFile) && !File.Exists(options.CollectorTokenFile))
            {
                problems.Add($"Notes uses the Http transport but the file named by {Section}:CollectorTokenFile does not exist.");
            }
        }

        return problems;
    }
}

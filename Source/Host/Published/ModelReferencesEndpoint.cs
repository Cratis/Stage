// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Publishing;

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Serves the model reference map to the frontend, so feedback can carry the identifiers of the elements it is about.
/// </summary>
internal static class ModelReferencesEndpoint
{
    /// <summary>
    /// The route the map is served on.
    /// </summary>
    internal const string Route = "/stage/model-references";

    internal static object Document(PackageManifest manifest) => new
    {
        applicationName = manifest.ApplicationName,
        publishedName = manifest.PublishedName,
        applicationVersion = manifest.ApplicationVersion,
        publicationId = manifest.PublicationId,
        references = manifest.ModelReferences
    };

    internal static void Map(WebApplication app, PackageManifest manifest) =>
        app.MapGet(Route, (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Json(Document(manifest), StageJson.Options);
        });
}

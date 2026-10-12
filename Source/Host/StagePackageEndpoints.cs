// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace Cratis.Stage.Host;

/// <summary>
/// Serves the custom runtime packages of a live Stage to the browser.
/// </summary>
internal static class StagePackageEndpoints
{
    /// <summary>
    /// The configuration key naming the folder of approved packages. In the container it is <c language="shell">/packages</c>.
    /// </summary>
    public const string PathKey = "Stage:Packages:Path";

    /// <summary>
    /// Reads the packages the host was configured with.
    /// </summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The packages, or <see langword="null"/> when none were provided.</returns>
    internal static StagePackages? Read(IConfiguration configuration) => StagePackages.Read(configuration[PathKey]);

    /// <summary>
    /// Serves the package document and the files of the approved packages, when there are packages.
    /// </summary>
    /// <param name="app">The application.</param>
    internal static void Map(WebApplication app)
    {
        var packages = Read(app.Configuration);

        // Always answer: a frontend that finds no packages renders with the shipped ones, and one that finds a
        // document with diagnostics refuses to render rather than render a profile with a package missing.
        app.MapGet(StagePackages.DocumentRoute, () => packages is null
            ? Results.Json(new { set = (object?)null, modules = new Dictionary<string, string>(), styles = Array.Empty<string>(), diagnostics = Array.Empty<string>() }, StageJson.Options)
            : Results.Json(packages.ToDocument(), StageJson.Options));

        if (packages is null)
        {
            return;
        }

        var files = new PhysicalFileProvider(packages.Root);
        var contentTypes = new FileExtensionContentTypeProvider();
        contentTypes.Mappings[".mjs"] = "text/javascript";
        contentTypes.Mappings[".map"] = "application/json";

        // Only the files of the packages the set approved are reachable, and only the types a package ships.
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(StagePackages.FilesRoute, out var remaining))
            {
                var relative = Uri.UnescapeDataString(remaining.Value ?? string.Empty).TrimStart('/');
                if (!packages.CanServe(relative))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }

            await next(context);
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            RequestPath = StagePackages.FilesRoute,
            ContentTypeProvider = contentTypes,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = context =>
            {
                context.Context.Response.Headers.CacheControl = "no-store";
                context.Context.Response.Headers.XContentTypeOptions = "nosniff";
            }
        });

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(StagePackages.FilesRoute))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(context);
        });
    }
}

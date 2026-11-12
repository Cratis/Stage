// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Mime;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Serves what loaded packages ship: the manifest's icon and images, the frontend, and each component's placeholder. Everything comes from the files read at load, which were
/// checked against the locked digest, and addresses name the digest, so nothing can be asked for outside the package and a response never changes.
/// </summary>
public static class ExtensionPackageEndpoints
{
    /// <summary>
    /// The route prefix frontend assets are served under. These are not tooling and are served wherever the packages are loaded.
    /// </summary>
    public const string AssetsRoute = "/extension-assets";

    /// <summary>
    /// The policy of a placeholder: it can show markup and inline styles and images that are part of it, and nothing else. No script runs, nothing outside it is loaded,
    /// and it is its own origin in a sandbox even when opened on its own.
    /// </summary>
    public const string PlaceholderPolicy = "default-src 'none'; style-src 'unsafe-inline'; img-src data:; font-src data:; base-uri 'none'; form-action 'none'; frame-ancestors 'self'; sandbox";

    /// <summary>
    /// The policy of an image: only itself.
    /// </summary>
    public const string ImagePolicy = "default-src 'none'; style-src 'unsafe-inline'; img-src data:; sandbox";

    /// <summary>
    /// Maps the endpoints that serve frontend assets.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="packages">The loaded packages.</param>
    public static void MapAssets(IEndpointRouteBuilder endpoints, ExtensionPackageSet packages) =>
        endpoints.MapGet($"{AssetsRoute}/{{id}}/{{**path}}", (HttpContext context, string id, string path) =>
            Serve(context, Find(packages, id), package => FrontendAsset(package, path), policy: null));

    /// <summary>
    /// Maps the tooling endpoints: the listing, and the icon, images and placeholders of each package.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="packages">The loaded packages.</param>
    public static void MapTooling(IEndpointRouteBuilder endpoints, ExtensionPackageSet packages)
    {
        var route = ExtensionPackageDescription.Route;
        endpoints.MapGet(route, () => Results.Json(
            new { Packages = packages.Loaded.Select(ExtensionPackageDescription.From), Refused = packages.Refusals.Select(refusal => new { refusal.Reference, Reason = refusal.Reason.ToString(), refusal.Details }) },
            StageJson.Options));
        endpoints.MapGet($"{route}/view", () => Results.Content(ExtensionPackageView.Render(packages), "text/html; charset=utf-8"));
        endpoints.MapGet($"{route}/packages/{{id}}/icon", (HttpContext context, string id) =>
            Serve(context, Find(packages, id), package => package.Contract.Icon is { } icon ? package.Contents.Find(Clean(icon)) is { } bytes ? (bytes, Clean(icon)) : null : null, ImagePolicy));
        endpoints.MapGet($"{route}/packages/{{id}}/images/{{index:int}}", (HttpContext context, string id, int index) =>
            Serve(context, Find(packages, id), package => index >= 0 && index < package.Contract.Images.Length && package.Contents.Find(Clean(package.Contract.Images[index].Path)) is { } bytes ? (bytes, Clean(package.Contract.Images[index].Path)) : null, ImagePolicy));
        endpoints.MapGet($"{route}/packages/{{id}}/placeholders/{{component}}", (HttpContext context, string id, string component) =>
            RefusesNavigation(context) ? Results.StatusCode(StatusCodes.Status403Forbidden) : Serve(context, Find(packages, id), package => Placeholder(package, component), PlaceholderPolicy));
    }

    static LoadedExtensionPackage? Find(ExtensionPackageSet packages, string id) => packages.Loaded.FirstOrDefault(package => package.Id == id);

    static bool RefusesNavigation(HttpContext context) =>
        context.Request.Headers.TryGetValue("Sec-Fetch-Dest", out var destination) && destination.Count > 0 && destination[0] is not ("iframe" or "frame" or "empty");

    static (byte[] Bytes, string Path)? Placeholder(LoadedExtensionPackage package, string component)
    {
        var declared = package.Contract.Components.FirstOrDefault(candidate => candidate.Name == component)?.Placeholder;
        return declared is not null && package.Contents.Find(Clean(declared)) is { } bytes ? (bytes, "placeholder.html") : null;
    }

    static (byte[] Bytes, string Path)? FrontendAsset(LoadedExtensionPackage package, string path)
    {
        if (package.Contract.Frontend is not { } entry) return null;

        var entryPath = Clean(entry);
        var folder = entryPath.Contains('/') ? entryPath[..(entryPath.LastIndexOf('/') + 1)] : string.Empty;
        var requested = folder + path;
        var servable = folder.Length > 0 ? requested.StartsWith(folder, StringComparison.Ordinal) && !path.Contains("..", StringComparison.Ordinal) : requested == entryPath;
        return servable && package.Contents.Find(requested) is { } bytes ? (bytes, requested) : null;
    }

    static string Clean(string path) => path.StartsWith("./", StringComparison.Ordinal) ? path[2..] : path;

    static IResult Serve(HttpContext context, LoadedExtensionPackage? package, Func<LoadedExtensionPackage, (byte[] Bytes, string Path)?> find, string? policy)
    {
        if (package is null || find(package) is not { } file) return Results.NotFound();

        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.CacheControl = "public, max-age=31536000, immutable";
        headers.ETag = $"\"{package.Id}:{PackageArtifact.Sha256(file.Bytes)}\"";
        headers["Referrer-Policy"] = "no-referrer";
        if (policy is not null) headers.ContentSecurityPolicy = policy;

        return Results.File(file.Bytes, ContentTypes.Of(file.Path));
    }

    static class ContentTypes
    {
        public static string Of(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".js" or ".mjs" => "text/javascript",
            ".css" => "text/css",
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".json" or ".map" => MediaTypeNames.Application.Json,
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".woff2" => "font/woff2",
            ".woff" => "font/woff",
            ".txt" => "text/plain; charset=utf-8",
            _ => MediaTypeNames.Application.Octet
        };
    }
}

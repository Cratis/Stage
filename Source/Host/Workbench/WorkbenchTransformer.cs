// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text;
using Yarp.ReverseProxy.Forwarder;

namespace Cratis.Stage.Host.Workbench;

/// <summary>
/// Strips the Workbench prefix off a forwarded request, and rewrites the HTML that comes back so the SPA and its
/// assets resolve against the prefix the browser actually reached.
/// </summary>
/// <param name="routePrefix">The path the Workbench is mapped under on this host.</param>
/// <param name="publicPrefix">The path the browser reaches the Workbench at, including any proxy path base.</param>
/// <param name="basePathMetaPattern">The unset <c>base-path</c> meta tag as the Workbench ships it.</param>
public sealed class WorkbenchTransformer(string routePrefix, string publicPrefix, string basePathMetaPattern) : HttpTransformer
{
    /// <inheritdoc/>
    public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
    {
        await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

        // The kernel serves the Workbench at its own root, so it must see the path below the prefix.
        var remainder = httpContext.Request.Path.StartsWithSegments(routePrefix, out var rest) && rest.HasValue
            ? rest
            : new PathString("/");

        proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(destinationPrefix, remainder, httpContext.Request.QueryString);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returning <see langword="false"/> for HTML takes over copying the body, which is what allows the rewrite. Everything
    /// else - assets, API responses, event streams - is left to stream through untouched.
    /// </remarks>
    public override async ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse, CancellationToken cancellationToken)
    {
        var copyBody = await base.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);

        if (!copyBody || proxyResponse is null || !IsHtml(proxyResponse.Content.Headers.ContentType))
        {
            return copyBody;
        }

        var html = Rewrite(await proxyResponse.Content.ReadAsStringAsync(cancellationToken));
        var body = Encoding.UTF8.GetBytes(html);

        // The length changed, and the entity is no longer the one the kernel produced.
        httpContext.Response.Headers.ContentLength = body.Length;
        httpContext.Response.Headers.Remove("ETag");
        httpContext.Response.Headers.Remove("Last-Modified");

        await httpContext.Response.Body.WriteAsync(body, cancellationToken);

        return false;
    }

    static bool IsHtml(MediaTypeHeaderValue? contentType) =>
        string.Equals(contentType?.MediaType, "text/html", StringComparison.OrdinalIgnoreCase);

    string Rewrite(string html) =>
        html.Replace(basePathMetaPattern, $"name=\"base-path\" content=\"{publicPrefix}\"", StringComparison.Ordinal)
            .Replace("src=\"/", $"src=\"{publicPrefix}/", StringComparison.Ordinal)
            .Replace("href=\"/", $"href=\"{publicPrefix}/", StringComparison.Ordinal);
}

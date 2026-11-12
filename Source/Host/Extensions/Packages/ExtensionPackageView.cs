// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Renders the page that lists loaded extensions. It holds no script and loads nothing from outside, so what a package says about itself can only be read, never run.
/// </summary>
public static class ExtensionPackageView
{
    /// <summary>
    /// Renders the page.
    /// </summary>
    /// <param name="packages">The loaded packages.</param>
    /// <returns>The HTML.</returns>
    public static string Render(ExtensionPackageSet packages)
    {
        var html = new StringBuilder("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta http-equiv=\"Content-Security-Policy\" content=\"default-src 'none'; style-src 'unsafe-inline'; img-src 'self'\">");
        html.Append("<title>Extensions</title><style>body{font-family:system-ui,sans-serif;margin:2rem}article{border:1px solid #ccc;border-radius:8px;padding:1rem;margin:1rem 0}img.icon{width:48px;height:48px;float:left;margin-right:1rem}dt{font-weight:600}.refused{border-color:#b00}</style></head><body><h1>Extensions</h1>");
        if (packages.Loaded.Count == 0 && packages.Refusals.Count == 0) html.Append("<p>This model uses no extension packages.</p>");

        foreach (var description in packages.Loaded.Select(ExtensionPackageDescription.From))
        {
            html.Append("<article>");
            if (description.Icon is not null) html.Append($"<img class=\"icon\" alt=\"\" src=\"{Encode(description.Icon)}\">");
            html.Append($"<h2>{Encode(description.DisplayName)} <small>{Encode(description.Version)}</small></h2><p>{Encode(description.Description)}</p><dl>");
            Row(html, "Reference", $"{description.Reference} ({description.Resolved})");
            Row(html, "Digest", description.Digest);
            Row(html, "Publisher", $"{description.Publisher}{(description.SignatureVerified ? " (signature verified)" : " (not signed)")}");
            Row(html, "Abilities", string.Join(", ", description.Contents.Abilities));
            Row(html, "Triggers", string.Join(", ", description.Contents.Triggers));
            Row(html, "Components", string.Join(", ", description.Contents.Components.Select(component => $"{component.Name} ({component.Kind ?? "unspecified"})")));
            Row(html, "Backend", description.Contents.Backend is { } backend ? $"{backend.Runtime} {backend.Version}" : "none");
            Row(html, "Requires", string.Join(", ", description.Requirements.Select(requirement => $"{requirement.Kind} {requirement.Value}")));
            html.Append("</dl>");
            foreach (var image in description.Images) html.Append($"<img alt=\"{Encode(image.Caption ?? string.Empty)}\" title=\"{Encode(image.Caption ?? string.Empty)}\" src=\"{Encode(image.Url)}\" width=\"240\">");
            html.Append("</article>");
        }

        foreach (var refusal in packages.Refusals)
        {
            html.Append($"<article class=\"refused\"><h2>{Encode(refusal.Reference)} was refused</h2><p><strong>{refusal.Reason}</strong>: {Encode(refusal.Details)}</p></article>");
        }

        return html.Append("</body></html>").ToString();
    }

    static void Row(StringBuilder html, string name, string value)
    {
        if (value.Length > 0) html.Append($"<dt>{Encode(name)}</dt><dd>{Encode(value)}</dd>");
    }

    static string Encode(string value) => WebUtility.HtmlEncode(value);
}

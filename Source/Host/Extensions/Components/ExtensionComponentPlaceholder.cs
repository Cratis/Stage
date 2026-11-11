// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Renders the static HTML for an extension component, which is all a page without a browser runtime (a preview, an export, the first paint) can hold.
/// The component itself runs only in the browser, so the placeholder carries what identifies it and nothing that needs the context.
/// </summary>
public static class ExtensionComponentPlaceholder
{
    /// <summary>
    /// Renders the placeholder.
    /// </summary>
    /// <param name="placement">The <see cref="ExtensionComponentPlacement"/>.</param>
    /// <returns>An HTML element.</returns>
    public static string Render(ExtensionComponentPlacement placement)
    {
        var name = $"{placement.System}.{placement.Component}";
        return $"<div data-stage-extension-component=\"{WebUtility.HtmlEncode(name)}\" data-kind=\"{placement.Kind.ToString().ToLowerInvariant()}\" "
            + $"role=\"region\" aria-label=\"{WebUtility.HtmlEncode(name)}\" aria-busy=\"true\"></div>";
    }
}

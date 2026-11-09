// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Names the settings of the signed forwarded identity an application opts into with <see cref="SignedForwardedIdentityInput"/>.
/// </summary>
/// <remarks>
/// Only names are rendered. The shared secret is never generated or written: the environment supplies it, and both keys are
/// declared as secrets in <see cref="RuntimeConfigurationFiles.EnvironmentPath"/>.
/// </remarks>
internal static class ForwardedIdentitySigning
{
    /// <summary>
    /// The environment key holding the secret Arc verifies the signature with, which configures
    /// <c language="csharp">Cratis:Arc:ForwardedIdentity:Keys:0:SecretBase64</c>.
    /// </summary>
    internal const string ArcSecretKey = "forwardedIdentity.arc.secretBase64";

    /// <summary>
    /// The environment key holding the secret AuthProxy signs with, which configures
    /// <c language="csharp">Cratis:AuthProxy:Services:&lt;name&gt;:ForwardedIdentity:SecretBase64</c>. It holds the same value as <see cref="ArcSecretKey"/>.
    /// </summary>
    internal const string AuthProxySecretKey = "forwardedIdentity.authProxy.secretBase64";

    /// <summary>
    /// The configuration path <see cref="ArcSecretKey"/> sets in the application.
    /// </summary>
    internal const string ArcSecretConfiguration = "Cratis:Arc:ForwardedIdentity:Keys:0:SecretBase64";

    /// <summary>
    /// The configuration path <see cref="AuthProxySecretKey"/> sets in AuthProxy. Stage does not name the service AuthProxy routes to
    /// the application, so the path keeps the <c language="csharp">&lt;name&gt;</c> placeholder of the AuthProxy documentation.
    /// </summary>
    internal const string AuthProxySecretConfiguration = "Cratis:AuthProxy:Services:<name>:ForwardedIdentity:SecretBase64";

    // AuthProxy accepts a key id of at most 64 characters; the longest suffix ('-api' on the audience) is four,
    // so both the key id and the audience stay within 64.
    const int MaximumNameLength = 60;

    internal static string Audience(string applicationName) => $"{Name(applicationName)}-api";

    internal static string KeyId(string applicationName) => $"{Name(applicationName)}-v1";

    // The Cratis:Arc:ForwardedIdentity settings of a signed application; null when the application is not signed.
    internal static JsonObject? Settings(string applicationName, bool signed) => signed
        ? new()
        {
            ["Enabled"] = true,
            ["Audience"] = Audience(applicationName),
            ["Keys"] = new JsonArray(new JsonObject { ["KeyId"] = KeyId(applicationName) })
        }
        : null;

    // Lower-cased, with every run of characters outside [a-z0-9] collapsed to one '-', so the key id fits [A-Za-z0-9_-].
    static string Name(string applicationName)
    {
        var name = new StringBuilder();
        foreach (var character in applicationName.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                name.Append(character);
            }
            else if (name.Length > 0 && name[^1] != '-')
            {
                name.Append('-');
            }
        }

        var text = name.ToString();
        text = text.Length > MaximumNameLength ? text[..MaximumNameLength] : text;
        text = text.Trim('-');
        return text.Length == 0 ? "application" : text;
    }
}

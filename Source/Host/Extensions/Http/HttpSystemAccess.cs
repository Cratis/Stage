// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Headers;
using System.Text;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// Resolves the base address and credentials of a system from its settings and secrets, and nowhere else.
/// </summary>
/// <remarks>
/// The settings are <c lang="text">systems:&lt;System&gt;:Http:BaseAddress</c>, <c lang="text">Authentication</c> (<c lang="text">none</c>, <c lang="text">bearer</c>, <c lang="text">basic</c> or <c lang="text">apiKey</c>),
/// <c lang="text">Token</c>, <c lang="text">Username</c>, <c lang="text">Password</c>, <c lang="text">ApiKey</c> and <c lang="text">ApiKeyHeader</c> (default <c lang="text">X-API-Key</c>).
/// </remarks>
/// <param name="settings">The <see cref="ISystemSettings"/>.</param>
public sealed class HttpSystemAccess(ISystemSettings settings)
{
    /// <summary>
    /// The prefix of every setting of the extension.
    /// </summary>
    public const string Prefix = "Http:";

    /// <summary>
    /// Resolves the access to a system.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The <see cref="HttpSystemConnection"/>, or a refusal that names the setting that is missing or wrong, never its value.</returns>
    public HttpSystemConnection Resolve(string system)
    {
        var address = settings.Resolve(system, $"{Prefix}BaseAddress");
        if (string.IsNullOrWhiteSpace(address)) return Refuse(system, "BaseAddress is not set");
        if (!Uri.TryCreate(address.EndsWith('/') ? address : address + "/", UriKind.Absolute, out var baseAddress) || baseAddress.Scheme is not ("http" or "https") || baseAddress.UserInfo.Length > 0)
        {
            return Refuse(system, "BaseAddress must be an http or https address without credentials");
        }

        var kind = (settings.Resolve(system, $"{Prefix}Authentication") ?? "none").Trim();
        switch (kind.ToLowerInvariant())
        {
            case "none":
                return new(baseAddress, _ => { }, null);
            case "bearer":
                if (Required(system, "Token") is not { } token) return Refuse(system, "Token is not set");
                return new(baseAddress, request => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token), null);
            case "basic":
                if (Required(system, "Username") is not { } user || Required(system, "Password") is not { } password) return Refuse(system, "Username and Password are not both set");
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}"));
                return new(baseAddress, request => request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded), null);
            case "apikey":
                if (Required(system, "ApiKey") is not { } key) return Refuse(system, "ApiKey is not set");
                var header = Required(system, "ApiKeyHeader") ?? "X-API-Key";
                return new(baseAddress, request => request.Headers.TryAddWithoutValidation(header, key), null);
            default:
                return Refuse(system, "Authentication must be none, bearer, basic or apiKey");
        }
    }

    static HttpSystemConnection Refuse(string system, string reason) => new(null, _ => { }, $"The setting systems:{system}:{Prefix}{reason.Split(' ')[0]} cannot be used: {reason}.");

    string? Required(string system, string name) => settings.Resolve(system, $"{Prefix}{name}") is { Length: > 0 } value ? value : null;
}

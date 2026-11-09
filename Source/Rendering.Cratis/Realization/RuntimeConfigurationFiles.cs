// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Realization;

/// <summary>
/// Renders the configuration files a runtime reads to start the services in front of and beside a rendered application.
/// </summary>
/// <remarks>
/// <c language="csharp">cratis run</c> and Stage Manager find them under <see cref="Folder"/> in the rendered application and add what only
/// they know: the environment name, the secrets directory and the values of the environment.
/// </remarks>
public static class RuntimeConfigurationFiles
{
    /// <summary>
    /// The folder, relative to the rendered application, holding the runtime configuration files.
    /// </summary>
    public const string Folder = ".runtime";

    /// <summary>
    /// The AuthProxy application contract: the provider list, in AuthProxy's own configuration shape.
    /// </summary>
    public const string AuthProxyPath = ".runtime/authproxy.application.json";

    /// <summary>
    /// The settings of the application's Ante, in the shape of its <c language="csharp">Ante:Onboarding</c> contract.
    /// </summary>
    public const string AntePath = ".runtime/ante.onboarding.json";

    /// <summary>
    /// The environment values the application needs, by key.
    /// </summary>
    public const string EnvironmentPath = ".runtime/environment.json";

    /// <summary>
    /// The name of the Ante event store the application observes. An application observes exactly one, so the runtime starts Ante with it.
    /// </summary>
    public const string AnteEventStore = "Ante";

    const string ProviderKeyPrefix = "authentication.providers.";
    const string AuthProxyEnvironmentConfiguration = "Cratis:AuthProxy:Application:Environment";

    static readonly string[] _providerKinds = ["EntraId", "GitHub", "Google", "Apple", "OpenId"];

    static readonly JsonSerializerOptions _indented = new() { WriteIndented = true, NewLine = "\n" };

    /// <summary>
    /// Gets whether AuthProxy can sign people in with a kind of identity provider.
    /// </summary>
    /// <param name="kind">The provider kind.</param>
    /// <returns>True when AuthProxy has the kind; otherwise false.</returns>
    public static bool IsSupportedKind(string kind) => _providerKinds.Contains(kind, StringComparer.Ordinal);

    /// <summary>
    /// Gets the providers AuthProxy can sign people in with.
    /// </summary>
    /// <param name="realization">The realization of the application.</param>
    /// <returns>The providers of a supported kind.</returns>
    public static ImmutableArray<RealizedProvider> SupportedProviders(ApplicationRealization realization) =>
        [.. realization.Providers.Where(provider => _providerKinds.Contains(provider.Kind, StringComparer.Ordinal))];

    /// <summary>
    /// Gets the providers AuthProxy has no kind for.
    /// </summary>
    /// <param name="realization">The realization of the application.</param>
    /// <returns>The providers that cannot be rendered.</returns>
    public static ImmutableArray<RealizedProvider> UnsupportedProviders(ApplicationRealization realization) =>
        [.. realization.Providers.Where(provider => !_providerKinds.Contains(provider.Kind, StringComparer.Ordinal))];

    /// <summary>
    /// Renders every runtime configuration file the realization calls for.
    /// </summary>
    /// <param name="realization">The realization of the application.</param>
    /// <param name="applicationName">The name of the application, which is the name of its Chronicle event store.</param>
    /// <returns>The relative path of each file and its content.</returns>
    public static IEnumerable<(string RelativePath, string Content)> Render(ApplicationRealization realization, string applicationName) =>
        Render(realization, applicationName, signedForwardedIdentity: false);

    /// <summary>
    /// Renders every runtime configuration file the realization calls for, declaring the secrets of signed forwarded identity when it is on.
    /// </summary>
    /// <param name="realization">The realization of the application.</param>
    /// <param name="applicationName">The name of the application, which is the name of its Chronicle event store.</param>
    /// <param name="signedForwardedIdentity">Whether the application verifies the signature of the identity AuthProxy forwards.</param>
    /// <returns>The relative path of each file and its content.</returns>
    public static IEnumerable<(string RelativePath, string Content)> Render(ApplicationRealization realization, string applicationName, bool signedForwardedIdentity)
    {
        var providers = SupportedProviders(realization);
        if (!providers.IsEmpty)
        {
            yield return (AuthProxyPath, AuthProxy(providers, realization.Onboarding is not null));
        }

        if (realization.Onboarding is { } onboarding)
        {
            yield return (AntePath, Ante(onboarding, applicationName));
        }

        // Nothing is signed without a provider AuthProxy signs people in with, as nothing is forwarded.
        var signed = signedForwardedIdentity && !providers.IsEmpty;
        if (!realization.Environment.IsDefaultOrEmpty || signed)
        {
            yield return (EnvironmentPath, Environment(realization, applicationName, signed));
        }
    }

    internal static string AuthProxy(ImmutableArray<RealizedProvider> providers, bool onboarding)
    {
        var application = new JsonObject
        {
            ["Providers"] = new JsonArray([.. providers.Select(provider => (JsonNode)new JsonObject { ["Kind"] = provider.Kind, ["Identity"] = provider.Identity })])
        };
        if (onboarding)
        {
            application["Onboarding"] = true;
        }

        return Serialize(new JsonObject { ["Cratis"] = new JsonObject { ["AuthProxy"] = new JsonObject { ["Application"] = application } } });
    }

    internal static string Ante(RealizedOnboarding onboarding, string applicationName)
    {
        var settings = new JsonObject
        {
            ["Enabled"] = true,
            ["HostStore"] = applicationName,
            ["EventStore"] = AnteEventStore,
            ["Namespace"] = "Default"
        };

        // Self-signup is off unless the model opens it, and Ante closes /register for it.
        settings["RegistrationEnabled"] = onboarding.Signup.Length > 0;
        if (onboarding.Signup.Length > 0)
        {
            settings["SignUp"] = onboarding.Signup == "JoinsTenant" ? "JoinTenant" : "CreateTenant";
        }

        var types = onboarding.Invitations.Select(invitation => invitation.Kind == "tenant" ? "CreateTenant" : "JoinTenant").Distinct(StringComparer.Ordinal).ToArray();
        if (types.Length > 0)
        {
            settings["InvitationTypes"] = new JsonArray([.. types.Select(type => (JsonNode)type)]);
        }

        var personas = GrantablePersonas(onboarding);
        if (personas.Length > 0)
        {
            settings["Personas"] = new JsonArray([.. personas.Select(persona => (JsonNode)persona)]);
        }

        if (!onboarding.LegalDocuments.IsDefaultOrEmpty)
        {
            // Ante presents two documents; the model lists them terms first, privacy second. Paths are relative to the model root.
            settings["Legal"] = onboarding.LegalDocuments.Length == 2
                ? new JsonObject { ["TermsAndConditions"] = onboarding.LegalDocuments[0], ["PrivacyPolicy"] = onboarding.LegalDocuments[1] }
                : new JsonObject { ["Documents"] = new JsonArray([.. onboarding.LegalDocuments.Select(document => (JsonNode)document)]) };
        }

        return Serialize(new JsonObject { ["Ante"] = new JsonObject { ["Onboarding"] = settings } });
    }

    internal static string[] GrantablePersonas(RealizedOnboarding onboarding) =>
        [.. onboarding.Invitations.SelectMany(invitation => invitation.Grant).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    internal static string Environment(ApplicationRealization realization, string applicationName, bool signedForwardedIdentity = false) =>
        Serialize(new JsonObject
        {
            ["application"] = applicationName,
            ["keys"] = new JsonArray([.. EnvironmentKeys(realization, signedForwardedIdentity).Select(EnvironmentEntry)])
        });

    // The target and configuration are written only when known.
    static JsonObject EnvironmentEntry(RealizedEnvironmentKey key)
    {
        var entry = new JsonObject { ["key"] = key.Key, ["secret"] = key.Secret };
        if (key.Target is not null)
        {
            entry["target"] = key.Target;
        }

        if (key.Configuration is not null)
        {
            entry["configuration"] = key.Configuration;
        }

        return entry;
    }

    // Only the names of the secrets of signed forwarded identity are declared; the runtime supplies their value.
    static ImmutableArray<RealizedEnvironmentKey> EnvironmentKeys(ApplicationRealization realization, bool signedForwardedIdentity)
    {
        var declared = realization.Environment.IsDefaultOrEmpty ? [] : realization.Environment.Select(AuthProxyProviderKey).ToImmutableArray();
        if (!signedForwardedIdentity)
        {
            return declared;
        }

        RealizedEnvironmentKey[] signing = [
            new(ForwardedIdentitySigning.ArcSecretKey, true) { Target = RealizedEnvironmentKey.ApplicationTarget, Configuration = ForwardedIdentitySigning.ArcSecretConfiguration },
            new(ForwardedIdentitySigning.AuthProxySecretKey, true) { Target = RealizedEnvironmentKey.AuthProxyTarget, Configuration = ForwardedIdentitySigning.AuthProxySecretConfiguration }
        ];
        return [.. declared, .. signing];
    }

    // AuthProxy reads a provider key from its application contract; the whole key, dots included, is one configuration key.
    static RealizedEnvironmentKey AuthProxyProviderKey(RealizedEnvironmentKey key) =>
        key.Key.StartsWith(ProviderKeyPrefix, StringComparison.Ordinal) && key.Configuration is null
            ? key with { Target = RealizedEnvironmentKey.AuthProxyTarget, Configuration = $"{AuthProxyEnvironmentConfiguration}:{key.Key}" }
            : key;

    static string Serialize(JsonNode node) => $"{node.ToJsonString(_indented)}\n";
}

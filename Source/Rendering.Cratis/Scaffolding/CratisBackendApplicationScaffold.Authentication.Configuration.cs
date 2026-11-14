// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

public partial class CratisBackendApplicationScaffold
{
    static string AuthenticationConfiguration(CratisBackendApplicationScaffoldRequest request) =>
        $$"""
        using Microsoft.Extensions.Configuration;
        using Microsoft.Identity.Abstractions;

        namespace {{request.RootNamespace}};

        internal static partial class GeneratedHostAuthentication
        {
            const string ConfigurationPath = "Stage:Authentication";

            internal sealed class InvalidHostAuthentication() : Exception("Invalid Stage:Authentication configuration. See the generated-host authentication documentation.");

            sealed record Settings(string Instance, string TenantId, string ClientId, string Audience, Uri PublicOrigin, CredentialDescription Credential);

            static Settings? ReadSettings(IConfiguration configuration)
            {
                var section = configuration.GetSection(ConfigurationPath);
                var enabled = section["EntraId:Enabled"];
                if (enabled is null || string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidHostAuthentication();
                }

                var entra = section.GetSection("EntraId");
                var tenant = Required(entra, "TenantId");
                var client = Required(entra, "ClientId");
                if (!Guid.TryParse(tenant, out var tenantId) || tenantId == Guid.Empty || !Guid.TryParse(client, out var clientId) || clientId == Guid.Empty)
                {
                    throw new InvalidHostAuthentication();
                }

                var instance = entra["Instance"] ?? "https://login.microsoftonline.com/";
                if (!TryOrigin(instance, out var authority) || authority.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidHostAuthentication();
                }

                var audience = Required(entra, "Audience");
                if (!TryOrigin(Required(section, "PublicOrigin"), out var origin) || origin.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidHostAuthentication();
                }

                if (!string.IsNullOrEmpty(entra["ClientSecret"]) || entra.GetSection("ClientCertificates").Exists())
                {
                    throw new InvalidHostAuthentication();
                }

                return new Settings(authority.AbsoluteUri, tenantId.ToString(), clientId.ToString(), audience, origin, ReadCredential(entra.GetSection("ClientCredentials")));
            }

            static string Required(IConfiguration section, string name)
            {
                var value = section[name];
                if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
                {
                    throw new InvalidHostAuthentication();
                }

                return value;
            }

            static CredentialDescription ReadCredential(IConfigurationSection section)
            {
                var entries = section.GetChildren().ToArray();
                if (entries.Length != 1)
                {
                    throw new InvalidHostAuthentication();
                }

                var entry = entries[0];
                if (entry.GetChildren().Any(child => child.Key is not ("SourceType" or "ManagedIdentityClientId" or "CertificateStorePath" or "CertificateThumbprint")))
                {
                    throw new InvalidHostAuthentication();
                }

                return entry["SourceType"] switch
                {
                    "SignedAssertionFromManagedIdentity" => ManagedIdentityCredential(entry),
                    "StoreWithThumbprint" => CertificateCredential(entry),
                    _ => throw new InvalidHostAuthentication()
                };
            }

            static CredentialDescription ManagedIdentityCredential(IConfiguration entry)
            {
                var identity = entry["ManagedIdentityClientId"];
                if (identity is not null && !IsNonEmptyGuid(identity))
                {
                    throw new InvalidHostAuthentication();
                }

                return new CredentialDescription { SourceType = CredentialSource.SignedAssertionFromManagedIdentity, ManagedIdentityClientId = identity };
            }

            static bool IsNonEmptyGuid(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty;

            static CredentialDescription CertificateCredential(IConfiguration entry)
            {
                var store = Required(entry, "CertificateStorePath");
                var thumbprint = Required(entry, "CertificateThumbprint");
                var validThumbprint = thumbprint.Length == 40 && thumbprint.All(Uri.IsHexDigit);
                if (store is not ("CurrentUser/My" or "LocalMachine/My") || !validThumbprint)
                {
                    throw new InvalidHostAuthentication();
                }

                return new CredentialDescription { SourceType = CredentialSource.StoreWithThumbprint, CertificateStorePath = store, CertificateThumbprint = thumbprint };
            }

            static bool TryOrigin(string value, out Uri origin)
            {
                origin = null!;
                if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed))
                {
                    return false;
                }

                var valid = HasOriginAuthority(parsed) && HasOriginPath(parsed) && HasNoSuffix(parsed);
                if (!valid)
                {
                    return false;
                }

                origin = parsed;
                return true;
            }

            static bool HasOriginAuthority(Uri value) => value.Scheme is "https" or "http" && !string.IsNullOrEmpty(value.Host);

            static bool HasOriginPath(Uri value) => string.IsNullOrEmpty(value.UserInfo) && value.AbsolutePath == "/";

            static bool HasNoSuffix(Uri value) => string.IsNullOrEmpty(value.Query) && string.IsNullOrEmpty(value.Fragment);
        }
        """;
}

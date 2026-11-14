// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineIssuer : IDisposable
{
    internal const string Tenant = "11111111-1111-4111-8111-111111111111";
    internal const string Client = "22222222-2222-4222-8222-222222222222";
    internal const string Audience = "api://offline-host";
    internal const string Origin = "https://app.example.test";
    internal const string Authority = $"https://login.microsoftonline.com/{Tenant}/v2.0";
    internal const string IssuerValidationClientName = "offline-issuer-validation";
    readonly RSA _rsa = RSA.Create(2048);
    readonly RsaSecurityKey _key;

    internal OfflineIssuer()
    {
        _key = new RsaSecurityKey(_rsa) { KeyId = "offline-host-signing-key" };
        var request = new CertificateRequest("CN=offline-host", _rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        Certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    internal X509Certificate2 Certificate { get; }
    internal string? Nonce { get; set; }
    internal string? IdTokenOverride { get; set; }
    internal int Redemptions { get; private set; }

    internal OpenIdConnectConfiguration Configuration
    {
        get
        {
            var configuration = new OpenIdConnectConfiguration
            {
                Issuer = Authority,
                AuthorizationEndpoint = $"{Authority}/authorize",
                TokenEndpoint = $"{Authority}/token",
                JwksUri = $"{Authority}/keys"
            };
            configuration.SigningKeys.Add(_key);
            return configuration;
        }
    }

    internal void ResetExchange()
    {
        Nonce = null;
        IdTokenOverride = null;
        Redemptions = 0;
    }

    internal string Token(TokenClaims? options = null)
    {
        options ??= new TokenClaims();
        var now = DateTime.UtcNow;
        return new JwtSecurityTokenHandler().CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer ?? Authority,
            Audience = options.Audience ?? Audience,
            Claims = Claims(options),
            NotBefore = now.AddHours(-2),
            Expires = options.Expired ? now.AddHours(-1) : now.AddMinutes(10),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256)
        });
    }

    internal Task<HttpResponseMessage> Respond(OfflineAuthenticationEndpoint endpoint, HttpRequestMessage request, CancellationToken cancellationToken) => endpoint switch
    {
        OfflineAuthenticationEndpoint.Metadata => Task.FromResult(Json(new { issuer = Authority, authorization_endpoint = Configuration.AuthorizationEndpoint, token_endpoint = Configuration.TokenEndpoint, jwks_uri = Configuration.JwksUri })),
        OfflineAuthenticationEndpoint.Keys => Task.FromResult(Keys()),
        OfflineAuthenticationEndpoint.InstanceDiscovery => Task.FromResult(Json(new
        {
            tenant_discovery_endpoint = $"{Authority}/.well-known/openid-configuration",
            metadata = new[] { new { preferred_network = "login.microsoftonline.com", preferred_cache = "login.microsoftonline.com", aliases = new[] { "login.microsoftonline.com" } } }
        })),
        OfflineAuthenticationEndpoint.Token => Redeem(request, cancellationToken),
        _ => throw new InvalidOperationException()
    };

    public void Dispose()
    {
        Certificate.Dispose();
        _rsa.Dispose();
    }

    HttpResponseMessage Keys()
    {
        var parameters = _rsa.ExportParameters(includePrivateParameters: false);
        return Json(new { keys = new[] { new { kty = "RSA", use = "sig", kid = _key.KeyId, n = Base64UrlEncoder.Encode(parameters.Modulus), e = Base64UrlEncoder.Encode(parameters.Exponent) } } });
    }

    static Dictionary<string, object> Claims(TokenClaims options)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = "offline-subject",
            ["oid"] = "33333333-3333-4333-8333-333333333333",
            ["tid"] = options.Tenant ?? Tenant,
            ["ver"] = "2.0"
        };
        if (options.Nonce is not null)
        {
            claims["nonce"] = options.Nonce;
        }

        if (options.Scope)
        {
            claims["scp"] = "access_as_user";
        }

        if (options.AppRole)
        {
            claims["idtyp"] = "app";
            claims["roles"] = new[] { "Service.Access" };
        }

        return claims;
    }

    static HttpResponseMessage Json(object content) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json") };

    async Task<HttpResponseMessage> Redeem(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not { } content)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        var canRedeem = request.Method == HttpMethod.Post && Nonce is not null;
        if (!canRedeem)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        var form = QueryHelpers.ParseQuery(await content.ReadAsStringAsync(cancellationToken));
        if (!IsValidCode(form))
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        Redemptions++;
        return Json(new
        {
            token_type = "Bearer",
            scope = "openid profile",
            expires_in = 3600,
            access_token = Token(),
            id_token = IdTokenOverride ?? Token(new TokenClaims { Audience = Client, Scope = false, Nonce = Nonce }),
            client_info = Base64UrlEncoder.Encode(JsonSerializer.Serialize(new { uid = "offline-subject", utid = Tenant }))
        });
    }

    static bool IsValidCode(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> form)
    {
        var pinned = form["redirect_uri"] == $"{Origin}/signin-oidc";
        var proof = !string.IsNullOrEmpty(form["code_verifier"]) && !string.IsNullOrEmpty(form["client_assertion"]);
        var code = form["grant_type"] == "authorization_code" && form["code"] == "offline-authorization-code";
        return pinned && proof && code;
    }
}

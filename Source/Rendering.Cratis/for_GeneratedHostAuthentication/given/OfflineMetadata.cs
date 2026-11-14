// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

static class OfflineMetadata
{
    internal static void Configure(WebApplicationBuilder builder, OfflineAuthenticationFixture fixture, HttpClient backchannel)
    {
        // Bind after emitted registration and before any option or token-acquisition service is resolved.
        // These are externally owned instances; collection teardown, never the host container, disposes them.
        builder.Services.RemoveAll<IHttpClientFactory>();
        builder.Services.RemoveAll<IMsalHttpClientFactory>();
        builder.Services.RemoveAll<IMsalMtlsHttpClientFactory>();
        builder.Services.RemoveAll<IMsalSFHttpClientFactory>();
        builder.Services.AddSingleton<IHttpClientFactory>(fixture.Transport);
        builder.Services.AddSingleton<IMsalHttpClientFactory>(fixture.Transport);
        builder.Services.AddSingleton<IMsalMtlsHttpClientFactory>(fixture.Transport);
        builder.Services.AddSingleton<IMsalSFHttpClientFactory>(fixture.Transport);

        // Preserve all framework validation and events. Replace only metadata/HTTP and the deployment
        // credential: MSAL still signs the assertion and performs the real code exchange with this certificate.
        var issuer = fixture.Issuer;
        builder.Services.PostConfigure<AadIssuerValidatorOptions>(options => options.HttpClientName = OfflineIssuer.IssuerValidationClientName);
        builder.Services.PostConfigure<MicrosoftIdentityOptions>(OfflineHost.OidcScheme, options => options.ClientCredentials =
            [new CredentialDescription { SourceType = CredentialSource.Certificate, Certificate = issuer.Certificate }]);
        builder.Services.PostConfigure<JwtBearerOptions>(OfflineHost.BearerScheme, options =>
        {
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(issuer.Configuration);
            var failed = options.Events.OnAuthenticationFailed;
            options.Events.OnAuthenticationFailed = async context =>
            {
                fixture.Diagnostics.Failure(context.Exception);
                await failed(context);
            };
        });
        builder.Services.PostConfigure<OpenIdConnectOptions>(OfflineHost.OidcScheme, options =>
        {
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(issuer.Configuration);
            options.Backchannel = backchannel;
            var messageReceived = options.Events.OnMessageReceived;
            options.Events.OnMessageReceived = async context =>
            {
                fixture.Diagnostics.Observe(OfflineAuthenticationPhase.OidcMessageReceived);
                await messageReceived(context);
            };
            var authorizationCodeReceived = options.Events.OnAuthorizationCodeReceived;
            options.Events.OnAuthorizationCodeReceived = async context =>
            {
                fixture.Diagnostics.Observe(OfflineAuthenticationPhase.AuthorizationCodeHandlerEntered);
                await authorizationCodeReceived(context);
                fixture.Diagnostics.Observe(OfflineAuthenticationPhase.AuthorizationCodeHandlerReturned);
            };
            var failed = options.Events.OnAuthenticationFailed;
            options.Events.OnAuthenticationFailed = async context =>
            {
                fixture.Diagnostics.Failure(context.Exception);
                await failed(context);
            };
            var remoteFailed = options.Events.OnRemoteFailure;
            options.Events.OnRemoteFailure = async context =>
            {
                if (context.Failure is { } failure)
                {
                    fixture.Diagnostics.Failure(failure);
                }

                await remoteFailed(context);
            };
        });
    }
}

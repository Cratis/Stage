// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;

namespace Cratis.Stage.Host;

/// <summary>
/// What identity the sandbox runs with.
/// </summary>
/// <param name="Enabled">Whether the sandbox plays a persona itself and refuses identity headers from callers.</param>
/// <param name="ForwardedIdentity">
/// Whether the sandbox runs behind AuthProxy (<c language="csharp">cratis run</c> starts it when the model has authentication) and so trusts the identity it forwards,
/// exactly as a rendered application does. The sandbox then plays no persona: whoever signed in is the caller.
/// </param>
internal sealed record SandboxIdentityOptions(bool Enabled, bool ForwardedIdentity = false)
{
    internal static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        // The identity headers are not signed, so trusting them is only right when every request arrives through AuthProxy, which strips what a
        // client sends and writes its own. A sandbox that plays a persona and one that trusts AuthProxy cannot both decide who the caller is.
        var forwarded = configuration.GetValue<bool>("Stage:Sandbox:ForwardedIdentity");
        services.AddSingleton(new SandboxIdentityOptions(configuration.GetValue<bool>("Stage:Sandbox:Identity") && !forwarded, forwarded));
        if (forwarded)
        {
            services.Configure<ArcOptions>(options => options.TrustForwardedIdentityHeaders = true);
        }
    }
}

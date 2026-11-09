// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // Modeled users are the one other case where the headers are trusted: a development tool such as Lens sends the user it selected. It is
        // development only and explicit (see SandboxUsersOptions), so the users and AuthProxy cannot both be on, and the users, who are the
        // caller the tool chose, take the place of the single persona Stage would pick for itself. UseSandboxIdentityHeaders
        // accepts the selected user only from the local machine unless remote access is allowed.
        var users = SandboxUsersOptions.Read(configuration, StageDevelopment.Decide(sandbox: true, configuration));
        if (users.Enabled && forwarded)
        {
            throw new InvalidOperationException($"{SandboxUsersOptions.Key} and Stage:Sandbox:ForwardedIdentity are both on. Modeled users are a development tool; behind AuthProxy the caller is whoever signed in.");
        }

        services.AddSingleton(users);
        services.TryAddSingleton(SandboxUsers.None);
        services.AddSingleton(new SandboxIdentityOptions(configuration.GetValue<bool>("Stage:Sandbox:Identity") && !forwarded && !users.Enabled, forwarded));
        if (forwarded || users.Enabled)
        {
            services.Configure<ArcOptions>(options => options.TrustForwardedIdentityHeaders = true);
        }
    }
}

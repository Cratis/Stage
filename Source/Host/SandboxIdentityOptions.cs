// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

internal sealed record SandboxIdentityOptions(bool Enabled)
{
    internal static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        var options = new SandboxIdentityOptions(configuration.GetValue<bool>("Stage:Sandbox:Identity"));
        services.AddSingleton(options);
    }
}

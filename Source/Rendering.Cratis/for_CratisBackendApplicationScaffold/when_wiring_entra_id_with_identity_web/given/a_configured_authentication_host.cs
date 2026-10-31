// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web.given;

public class a_configured_authentication_host : an_emitted_authentication_host
{
    void Establish()
    {
        ConfigureEntraId();
        AddAuthentication();
        _app = _builder.Build();
    }

    protected OpenIdConnectOptions OpenIdConnectOptions() =>
        _app.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web.given;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_wiring_entra_id_with_identity_web;

public class without_configuration : an_emitted_authentication_host
{
    int _servicesBefore;
    int _servicesAfter;
    HttpStatusCode _status;

    async Task Because()
    {
        _servicesBefore = _builder.Services.Count;
        AddAuthentication();
        _servicesAfter = _builder.Services.Count;
        _app = _builder.Build();
        UseAuthentication();
        _app.MapGet("/unprotected", () => "Available");
        await _app.StartAsync();
        using var client = _app.GetTestClient();
        using var response = await client.GetAsync("/unprotected");
        _status = response.StatusCode;
    }

    [Fact] void should_leave_the_service_collection_unchanged() => _servicesAfter.ShouldEqual(_servicesBefore);
    [Fact] void should_leave_an_unauthenticated_endpoint_accessible() => _status.ShouldEqual(HttpStatusCode.OK);
}

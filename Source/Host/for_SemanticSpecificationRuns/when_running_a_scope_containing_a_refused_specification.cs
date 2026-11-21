// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_a_scope_containing_a_refused_specification : a_specification_endpoint
{
    protected override string Source => for_SemanticHost.given.a_refused_specification.Source;

    [Theory]
    [InlineData("all")]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("slice")]
    public async Task should_not_silently_pass_a_scope_missing_its_broken_specification(string scope)
    {
        var application = _loaded.Model.Application;
        var feature = application.Modules.Single().Features.Single();
        var identity = scope switch
        {
            "application" => application.Id.ToString(),
            "module" => application.Modules.Single().Id.ToString(),
            "feature" => feature.Id.ToString(),
            _ => feature.Slices.Single().Id.ToString()
        };
        await Request(scope == "all" ? "{\"scopes\":[]}" : $$"""{"scopes":["{{identity}}"]}""");
        _status.ShouldEqual(StatusCodes.Status501NotImplemented);
        JsonElement.Parse(_body).GetProperty("issues")[0].GetString().ShouldEqual(_loaded.RefusedSpecifications.Single().Diagnostics[0].Message);
    }
}

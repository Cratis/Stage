// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_a_refused_specification : a_specification_endpoint
{
    protected override string Source => for_SemanticHost.given.a_refused_specification.Source;

    async Task Because() => await Request($$"""{"scopes":["{{_loaded.RefusedSpecifications.Single().Id}}"]}""");

    [Fact] void should_refuse_only_the_requested_operation() => _status.ShouldEqual(StatusCodes.Status501NotImplemented);
    [Fact] void should_preserve_the_original_error_message() => JsonElement.Parse(_body).GetProperty("issues")[0].GetString().ShouldEqual(_loaded.RefusedSpecifications.Single().Diagnostics[0].Message);
    [Fact] void should_preserve_the_original_error_code() => _body.ShouldContain(_loaded.RefusedSpecifications.Single().Diagnostics[0].Code);
    [Fact] void should_not_report_a_passing_run() => _body.ShouldNotContain("stage-spec-run/1");
}

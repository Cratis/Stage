// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_compiling_generated_shapes : Specification
{
    [Fact] void should_report_play0483_for_an_optional_property() => Failure("Id?").ShouldContain("PLAY0483");
    [Fact] void should_report_play0483_for_a_collection_property() => Failure("Id[]").ShouldContain("PLAY0483");
    [Fact] void should_report_play0483_for_a_primitive_property() => Failure("Uuid").ShouldContain("PLAY0483");

    static string Failure(string type) =>
        Catch.Exception(() => invoice_model.Compile($"concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id {type} generated identifier\n        returns id"))?.Message ?? string.Empty;
}

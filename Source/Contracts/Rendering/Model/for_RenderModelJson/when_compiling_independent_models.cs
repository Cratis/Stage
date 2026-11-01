// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson;

public class when_compiling_independent_models : given.render_models
{
    const string First = "      command FirstEcho\n        message String\n        returns message";
    const string Second = "      command SecondEcho\n        reply String\n        returns reply";
    byte[] _first = [];
    byte[] _second = [];

    void Because()
    {
        _first = RenderModelJson.Serialize(RenderModelBuilder.Build(CompileRequest(OrderedSource(reversed: false))));
        _second = RenderModelJson.Serialize(RenderModelBuilder.Build(CompileRequest(OrderedSource(reversed: true))));
    }

    [Fact] void should_ignore_declaration_order_across_separate_compilations() => EqualBytes(_first, _second);
    [Fact] void should_rebuild_identical_bytes_from_the_same_source() => EqualBytes(_first, RenderModelJson.Serialize(RenderModelBuilder.Build(CompileRequest(OrderedSource(reversed: false)))));

    static string OrderedSource(bool reversed) => "module Messaging\n  feature Echoes\n    slice StateChange Reply\n" + (reversed ? Second + "\n" + First : First + "\n" + Second);
}

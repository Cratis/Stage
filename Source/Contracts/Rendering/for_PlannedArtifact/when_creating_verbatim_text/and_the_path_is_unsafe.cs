// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_PlannedArtifact.when_creating_verbatim_text;

public class and_the_path_is_unsafe : Specification
{
    Exception _verbatim = null!;
    Exception _text = null!;

    void Because()
    {
        _verbatim = Catch.Exception(() => PlannedArtifact.CreateVerbatimText("../escape.txt", [0x61]));
        _text = Catch.Exception(() => PlannedArtifact.CreateText("../escape.txt", "a"));
    }

    [Fact] void should_throw_an_invalid_contract() => _verbatim.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_use_the_same_validation_as_text() => _verbatim.Message.ShouldEqual(_text.Message);
}

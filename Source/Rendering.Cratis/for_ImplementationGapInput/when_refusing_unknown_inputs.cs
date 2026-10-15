// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ImplementationGapInput;

public class when_refusing_unknown_inputs : Specification
{
    bool[] _results = null!;
    Exception _failure = null!;
    void Because()
    {
        _failure = Catch.Exception(() => ImplementationGapInput.Create((ImplementationGapMode)99));
        ArtifactRenderInput[] inputs =
        [
            null!, Input("other", "1", "Stub"), Input(ImplementationGapInput.Name, "2", "Stub"),
            Input(ImplementationGapInput.Name, "1", "stub"), Input(ImplementationGapInput.Name, "1", "Stub\n"),
            Input(ImplementationGapInput.Name, "1", "1"), Input(ImplementationGapInput.Name, "1", "")
        ];
        _results = [.. inputs.Select(input => ImplementationGapInput.TryRead(input, out var mode) || mode != ImplementationGapMode.Refuse)];
    }

    [Fact] void should_refuse_unknown_modes() => _failure.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_examine_seven_invalid_inputs() => _results.Length.ShouldEqual(7);
    [Fact] void should_refuse_every_invalid_input_and_leave_refuse_as_the_out_value() => _results.Any(result => result).ShouldBeFalse();
    static ArtifactRenderInput Input(string name, string version, string text) => ArtifactRenderInput.Create(name, version, [.. Encoding.UTF8.GetBytes(text)]);
}

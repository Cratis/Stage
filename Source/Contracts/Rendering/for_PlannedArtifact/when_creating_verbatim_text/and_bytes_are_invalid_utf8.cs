// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_PlannedArtifact.when_creating_verbatim_text;

public class and_bytes_are_invalid_utf8 : Specification
{
    Exception _exception = null!;

    void Because() => _exception = Catch.Exception(() => PlannedArtifact.CreateVerbatimText("a.txt", [0x61, 0xFF, 0xFE]));

    [Fact] void should_throw_an_invalid_contract() => _exception.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}

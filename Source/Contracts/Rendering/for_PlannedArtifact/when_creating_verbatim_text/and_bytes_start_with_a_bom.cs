// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_PlannedArtifact.when_creating_verbatim_text;

public class and_bytes_start_with_a_bom : Specification
{
    Exception _exception = null!;

    void Because() => _exception = Catch.Exception(() => PlannedArtifact.CreateVerbatimText("a.txt", [0xEF, 0xBB, 0xBF, 0x61]));

    [Fact] void should_throw_an_invalid_contract() => _exception.ShouldBeOfExactType<InvalidArtifactRenderContract>();
}

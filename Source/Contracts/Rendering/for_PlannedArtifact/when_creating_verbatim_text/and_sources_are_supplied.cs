// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_PlannedArtifact.when_creating_verbatim_text;

public class and_sources_are_supplied : Specification
{
    SemanticId _source;
    PlannedArtifact _artifact = null!;

    void Establish() => _source = SemanticId.Create(SemanticAddress.ForApplication(ApplicationIdentity.Create("Projects")));

    void Because() => _artifact = PlannedArtifact.CreateVerbatimText("a.txt", [0x61, 0x0D], [_source]);

    [Fact] void should_record_the_sources() => _artifact.Sources.SequenceEqual([_source]).ShouldBeTrue();
    [Fact] void should_keep_the_bytes_unchanged() => _artifact.Bytes.AsSpan().SequenceEqual("a\r"u8).ShouldBeTrue();
}

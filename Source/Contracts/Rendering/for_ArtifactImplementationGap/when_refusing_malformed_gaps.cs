// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_refusing_malformed_gaps : given.a_gap
{
    Exception _defaultGaps = null!;
    Exception _nullGap = null!;
    Exception _blankRequirement = null!;
    Exception _nullRequirement = null!;
    Exception _blankOwner = null!;
    Exception _unknownOrigin = null!;
    Exception _unknownRole = null!;
    Exception _defaultHints = null!;
    Exception _nullHint = null!;
    Exception _contextVersion = null!;
    Exception _resultVersion = null!;
    Exception _emptyStubs = null!;
    Exception _defaultStubs = null!;
    Exception _nullStub = null!;
    Exception _duplicateGap = null!;

    void Because()
    {
        _defaultGaps = Catch.Exception(() => Digest(default));
        _nullGap = Catch.Exception(() => Digest([null!]));
        _blankRequirement = Refuse(_gap with { RequirementId = "" });
        _nullRequirement = Refuse(_gap with { RequirementId = null! });
        _blankOwner = Refuse(_gap with { Owner = " " });
        _unknownOrigin = Refuse(_gap with { Origin = ArtifactImplementationGapOrigin.Unknown });
        _unknownRole = Refuse(_gap with { Role = (SemanticImplementationRole)999 });
        _defaultHints = Refuse(_gap with { Hints = default });
        _nullHint = Refuse(_gap with { Hints = [null!] });
        _contextVersion = Refuse(_gap with { ContextVersion = 0 });
        _resultVersion = Refuse(_gap with { ResultVersion = 2 });
        _emptyStubs = Refuse(_gap with { Stubs = [] });
        _defaultStubs = Refuse(_gap with { Stubs = default });
        _nullStub = Refuse(_gap with { Stubs = [null!] });
        _duplicateGap = Catch.Exception(() => Digest([_gap, _gap]));
    }

    [Fact] void should_refuse_default_gap_collections() => _defaultGaps.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_null_gaps() => _nullGap.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_blank_requirement_ids() => _blankRequirement.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_null_requirement_ids() => _nullRequirement.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_blank_owners() => _blankOwner.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_unknown_origins() => _unknownOrigin.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_unknown_roles() => _unknownRole.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_default_hints() => _defaultHints.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_null_hints() => _nullHint.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_unsupported_context_versions() => _contextVersion.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_unsupported_result_versions() => _resultVersion.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_empty_stubs() => _emptyStubs.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_default_stubs() => _defaultStubs.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_null_stubs() => _nullStub.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_duplicate_requirement_ids() => _duplicateGap.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    Exception Refuse(ArtifactImplementationGap gap) => Catch.Exception(() => Digest([gap]))!;
}

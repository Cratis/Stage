// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_refusing_malformed_stubs : given.a_gap
{
    Exception _traversal = null!;
    Exception _blankType = null!;
    Exception _blankMember = null!;
    Exception _crSignature = null!;
    Exception _missingLocator = null!;
    Exception _missingContext = null!;
    Exception _defaultAcceptance = null!;
    Exception _blankAcceptance = null!;
    Exception _duplicate = null!;
    Exception _overlapping = null!;
    Exception _sharedBody = null!;

    void Because()
    {
        _traversal = Refuse(_stub with { RelativePath = "../other.cs" });
        _blankType = Refuse(_stub with { TypeName = "" });
        _blankMember = Refuse(_stub with { MemberName = "" });
        _crSignature = Refuse(_stub with { Signature = "signature\r\n" });
        _missingLocator = Refuse(_stub with { BodyLocator = null! });
        _missingContext = Refuse(_stub with { TypedContext = null! });
        _defaultAcceptance = Refuse(_stub with { AcceptanceSpecifications = default });
        _blankAcceptance = Refuse(_stub with { AcceptanceSpecifications = [""] });
        _duplicate = Catch.Exception(() => Digest([_gap with { Stubs = [_stub, _stub] }]));
        _overlapping = Catch.Exception(() => Digest([_gap with { Stubs = [_stub, _stub with { MemberName = "Other", BodyLocator = new(121, 80) }] }]));
        _sharedBody = Catch.Exception(() => Digest([_gap, _gap with { RequirementId = "other" }]));
    }

    [Fact] void should_refuse_traversal_paths() => _traversal.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_blank_types() => _blankType.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_blank_members() => _blankMember.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_non_lf_signatures() => _crSignature.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_missing_locators() => _missingLocator.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_missing_contexts() => _missingContext.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_default_acceptance_classes() => _defaultAcceptance.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_blank_acceptance_classes() => _blankAcceptance.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_duplicate_stubs() => _duplicate.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_overlapping_bodies() => _overlapping.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_bodies_shared_by_requirements() => _sharedBody.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    Exception Refuse(ArtifactImplementationStub stub)
    {
        var exception = Catch.Exception(() => Digest([_gap with { Stubs = [stub] }]));
        exception.ShouldNotBeNull();
        return exception;
    }
}

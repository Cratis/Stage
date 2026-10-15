// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_computing_a_gap_digest : given.a_gap
{
    string _digest = null!;
    void Because() => _digest = Digest([_gap]);

    [Fact] void should_change_the_empty_plan_digest() => _digest.ShouldNotEqual(Digest([]));
    [Fact] void should_be_deterministic() => Digest([_gap]).ShouldEqual(_digest);
    [Fact] void should_include_the_requirement_identity() => Digest([_gap with { RequirementId = "another" }]).ShouldNotEqual(_digest);
    [Fact] void should_include_the_owner() => Digest([_gap with { Owner = "another-owner" }]).ShouldNotEqual(_digest);
    [Fact] void should_include_ordered_hints() => Digest([_gap with { Hints = ["second", "first"] }]).ShouldNotEqual(Digest([_gap with { Hints = ["first", "second"] }]));
    [Fact] void should_include_the_path() => Changed(_stub with { RelativePath = "other.cs" }).ShouldNotEqual(_digest);
    [Fact] void should_include_the_type() => Changed(_stub with { TypeName = "Projects.OtherPayloads" }).ShouldNotEqual(_digest);
    [Fact] void should_include_the_member() => Changed(_stub with { MemberName = "Other" }).ShouldNotEqual(_digest);
    [Fact] void should_include_the_exact_signature() => Changed(_stub with { Signature = _stub.Signature + " " }).ShouldNotEqual(_digest);
    [Fact] void should_include_the_body_start() => Changed(_stub with { BodyLocator = new(121, 80) }).ShouldNotEqual(_digest);
    [Fact] void should_include_the_body_length() => Changed(_stub with { BodyLocator = new(120, 81) }).ShouldNotEqual(_digest);
    [Fact] void should_include_the_context_type() => Changed(_stub with { TypedContext = _stub.TypedContext with { TypeName = "Projects.OtherContext" } }).ShouldNotEqual(_digest);
    [Fact] void should_include_context_member_types() => Changed(_stub with { TypedContext = _stub.TypedContext with { Members = [new("Command", "Projects.Other", true)] } }).ShouldNotEqual(_digest);
    [Fact] void should_include_context_member_names() => Changed(_stub with { TypedContext = _stub.TypedContext with { Members = [_stub.TypedContext.Members[0] with { Name = "Other" }, _stub.TypedContext.Members[1]] } }).ShouldNotEqual(_digest);
    [Fact] void should_include_context_realization() => Changed(_stub with { TypedContext = _stub.TypedContext with { Members = [_stub.TypedContext.Members[0] with { IsRealized = false }, _stub.TypedContext.Members[1]] } }).ShouldNotEqual(_digest);
    [Fact] void should_include_acceptance_classes() => Changed(_stub with { AcceptanceSpecifications = [] }).ShouldNotEqual(_digest);
    [Fact] void should_ignore_portable_path_separators() => Changed(_stub with { RelativePath = "Features\\RegisterPayloads.cs" }).ShouldEqual(_digest);
    [Fact] void should_ignore_acceptance_class_enumeration_order() => Changed(_stub with { AcceptanceSpecifications = ["A", "B"] }).ShouldEqual(Changed(_stub with { AcceptanceSpecifications = ["B", "A", "B"] }));
    [Fact] void should_ignore_gap_enumeration_order() => Digest([_gap, OtherGap()]).ShouldEqual(Digest([OtherGap(), _gap]));
    [Fact] void should_ignore_stub_enumeration_order() => Digest([_gap with { Stubs = [_stub, _stub with { RelativePath = "other.cs" }] }]).ShouldEqual(Digest([_gap with { Stubs = [_stub with { RelativePath = "other.cs" }, _stub] }]));

    string Changed(ArtifactImplementationStub stub) => Digest([_gap with { Stubs = [stub] }]);
    ArtifactImplementationGap OtherGap() => _gap with { RequirementId = "requirement-2", Stubs = [_stub with { RelativePath = "other.cs" }] };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_refusing_malformed_contexts : given.a_gap
{
    Exception _blankType = null!;
    Exception _defaultMembers = null!;
    Exception _duplicateNames = null!;
    Exception _nullMember = null!;
    Exception _blankMemberName = null!;
    Exception _blankMemberType = null!;
    void Because()
    {
        _blankType = Refuse(new("", []));
        _defaultMembers = Refuse(new("Context", default));
        _duplicateNames = Refuse(new("Context", [new("Name", "Type", true), new("Name", "Type", false)]));
        _nullMember = Refuse(new("Context", [null!]));
        _blankMemberName = Refuse(new("Context", [new("", "Type", true)]));
        _blankMemberType = Refuse(new("Context", [new("Name", "", true)]));
    }

    [Fact] void should_refuse_blank_context_types() => _blankType.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_default_members() => _defaultMembers.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_duplicate_member_names() => _duplicateNames.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_null_members() => _nullMember.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_blank_member_names() => _blankMemberName.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_refuse_blank_member_types() => _blankMemberType.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    Exception Refuse(ArtifactImplementationContext context)
    {
        var exception = Catch.Exception(() => Digest([_gap with { Stubs = [_stub with { TypedContext = context }] }]));
        exception.ShouldNotBeNull();
        return exception;
    }
}

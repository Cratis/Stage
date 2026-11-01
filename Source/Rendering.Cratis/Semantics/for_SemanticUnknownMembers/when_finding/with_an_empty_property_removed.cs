// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticUnknownMembers.when_finding;

public class with_an_empty_property_removed : a_populated_model
{
    void Because()
    {
        _known.Remove("SemanticApplication.Triggers");
        _known.Remove("SemanticCommand.Handler");
        _known.Remove("SemanticApplication.ReactionProductionMode");
        _unknown = SemanticUnknownMembers.Find(_model, _known);
    }

    [Fact] void should_ignore_empty_collections_and_absent_optional_members() => _unknown.ShouldBeEmpty();
}

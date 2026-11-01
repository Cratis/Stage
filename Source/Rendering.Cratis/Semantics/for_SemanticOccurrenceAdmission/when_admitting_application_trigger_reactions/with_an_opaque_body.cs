// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_an_opaque_body : given.an_application_trigger_reaction
{
    void Establish() => _trigger = _trigger with { RequirementId = "opaque-reaction", Produces = [] };
    void Because() => Inspect();

    [Fact]
    void should_keep_the_generic_opaque_body_refusal()
    {
        var diagnostic = _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Message.Contains("opaque body", StringComparison.Ordinal));
        diagnostic.Message.ShouldContain("ApplicationTrigger");
    }
}

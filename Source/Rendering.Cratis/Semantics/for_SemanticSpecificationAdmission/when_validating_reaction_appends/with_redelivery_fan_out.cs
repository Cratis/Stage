// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_redelivery_fan_out : for_SemanticReactionSpecificationRenderer.given.a_redelivery_specification
{
    void Establish()
    {
        var worker = _located.Slice.Reactions[0];
        ReplaceAutomation(_located.Slice with { Reactions = [worker with { Triggers = [worker.Triggers[0], worker.Triggers[0]] }, _located.Slice.Reactions[1]] });
    }

    void Because() => Validate();
    [Fact] void should_refuse_multiple_triggers_on_the_named_reaction() => _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-011").Message.ShouldContain("fan-out requires a settlement harness");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_deeper_redelivery_cascade : for_SemanticReactionSpecificationRenderer.given.a_redelivery_specification
{
    void Establish() => Prepare(RedeliverySource + "\n      reaction Next\n        when Followed\n");
    void Because() => Validate();
    [Fact] void should_refuse_unsettled_redelivery_descendants() => _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-011").Message.ShouldContain("do not settle deeper reaction cascades");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_context : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare(ProductionSource.Replace("      event Followed\n        amount Decimal", "      event Followed\n        scheduledAt DateTime", StringComparison.Ordinal)
        .Replace("            amount = amount", "            scheduledAt = $context.occurred", StringComparison.Ordinal)
        .Replace("        then Followed\n          amount = 5", "        then Followed\n          scheduledAt = \"2026-10-01T00:00:00.0000000+00:00\"", StringComparison.Ordinal));
    void Because() => Validate();
    [Fact] void should_refuse_uncontrolled_event_context() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}

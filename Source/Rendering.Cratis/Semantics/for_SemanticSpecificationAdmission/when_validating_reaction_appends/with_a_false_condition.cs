// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_false_condition : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish()
    {
        Prepare(ProductionSource.Replace("      reaction Worker", "      reaction Worker\n        where amount > 10", StringComparison.Ordinal));
    }
    void Because() => Validate();
    [Fact] void should_refuse_an_append_without_an_active_trigger() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011");
}

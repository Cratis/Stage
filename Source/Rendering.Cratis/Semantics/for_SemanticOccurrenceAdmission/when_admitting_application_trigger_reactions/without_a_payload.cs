// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class without_a_payload : given.an_application_trigger_reaction
{
    void Because() => InspectSource(Source.Replace("  batchId String\nmodule", "module", StringComparison.Ordinal)
        .Replace("          batchId\n", string.Empty, StringComparison.Ordinal)
        .Replace("for batchId", "for \"one\"", StringComparison.Ordinal)
        .Replace("batchId = batchId", "batchId = \"one\"", StringComparison.Ordinal));

    [Fact] void should_admit_literal_productions_without_a_payload() => _diagnostics.ShouldBeEmpty();
}

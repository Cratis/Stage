// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_pinned_refusal_scenarios;

public class with_an_unpinned_generated_property : given.a_specification_in_a_slice
{
    void Establish()
    {
        var unpinned = with_a_pinned_generated_property.Source
            .Replace("        given generated Complete.ticket = \"5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01\"\n", string.Empty, StringComparison.Ordinal);
        Prepare(unpinned[..unpinned.IndexOf("        then Completed", StringComparison.Ordinal)] + "        then no events\n");
    }

    void Because() => Validate();

    [Fact] void should_refuse_the_specification() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_ask_for_the_pin() => _diagnostics.Single().Message.ShouldContain("given generated <Command>.<property>");
}

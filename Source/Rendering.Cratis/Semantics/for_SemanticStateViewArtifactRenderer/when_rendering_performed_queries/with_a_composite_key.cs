// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_rendering_performed_queries;

public class with_a_composite_key : given.a_performed_query_application
{
    void Establish() => Prepare(
        """
        module Billing
          feature Invoices
            slice StateView Ledger
              readmodel Ledger
                book String key
                page Int key
                total Int
              query LedgerPage => Ledger optional
                by
                  book String
                  page Int
                performer
                  file Queries/LedgerPage.cs
        """);

    void Because() => Evaluate();


    [Fact] void should_refuse_the_state_view() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-007");
    [Fact] void should_name_the_query() => _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-007").Message.ShouldContain("Performer query 'LedgerPage' cannot render");
    [Fact] void should_say_why() => _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-007").Message.ShouldContain("composite 'by' key");
}

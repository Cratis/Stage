// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_a_wrong_unordered_event : Specification
{
    bool _success;
    string[] _codes = null!;

    void Because()
    {
        var source = with_unordered_events.Source.Replace(
            "InvoiceRecorded\n          description = \"First payload\"",
            "InvoiceRecorded\n          description = \"Wrong payload\"",
            StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("InvoiceModel"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("invoices"), "invoices", "Invoices.play", source);
        var result = new SemanticModelCompiler().Compile("InvoiceModel", SemanticDocumentSet.Create([document], catalog));
        _success = result.Success;
        _codes = [.. result.Diagnostics.Select(_ => _.Code)];
    }

    [Fact] void should_reject_a_wrong_multiset_member_before_rendering() => _success.ShouldBeFalse();
    [Fact] void should_name_the_unproducible_expectation() => _codes.ShouldContain("PLAY0285");
}
#endif

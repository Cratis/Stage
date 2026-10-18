// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticConditionRendering;

public class when_refusing_uuid_reaction_guards : Specification
{
    [Theory]
    [InlineData("==")]
    [InlineData("!=")]
    public void should_refuse_the_guard_before_a_render_plan_can_be_created(string comparison)
    {
        var source = for_SemanticAutomationArtifactRenderer.given.an_automation.Source
            .Replace("amount Decimal", "amount Uuid", StringComparison.Ordinal)
            .Replace("          id String = id\n", string.Empty, StringComparison.Ordinal)
            .Replace("            id = id", "            id = $eventSourceId", StringComparison.Ordinal)
            .Replace("where amount > 0", $"where amount {comparison} \"a1111111-1111-4111-8111-111111111111\"", StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Reactions"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("reactions"), "reactions", "Reactions.play", source);
        var compilation = new SemanticModelCompiler().Compile("Reactions", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeFalse();
        compilation.Value.ShouldBeNull();
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly("PLAY0268");
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeEmpty();
    }
}

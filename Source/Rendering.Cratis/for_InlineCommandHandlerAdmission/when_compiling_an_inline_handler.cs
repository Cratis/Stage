// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_InlineCommandHandlerAdmission;

public class when_compiling_an_inline_handler
{
    const string Source = """
        module Billing
          feature Invoices
            slice StateChange ProcessBatch
              command ProcessBatch
                handler
                  csharp
                    ```
                    return new object[] { context.Identity.Id };
                    ```
        """;

    [Fact]
    public void should_keep_context_dependent_inline_syntax_accepted_by_the_screenplay_compiler()
    {
        var result = new ScreenplayCompiler().Compile(Source);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!.Code!.Code.ShouldContain("context.Identity.Id");
    }

    [Fact]
    public void should_keep_the_esm_binder_rejection_of_inline_handlers()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("inline"), "inline", "Inline.play", Source);
        var result = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("handler requires a constrained implementation attachment", StringComparison.Ordinal));
    }
}

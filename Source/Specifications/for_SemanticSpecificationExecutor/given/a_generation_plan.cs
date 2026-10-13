// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public static class a_generation_plan
{
    public static SemanticExecutionPlan Compile(string primitive, string generation, string fixture, string outcome, string prior = "", bool v15 = true)
    {
        var source = $$"""
            concept Id : {{primitive}}
            {{generation}}
            {{(v15 ? "concept Receipt : String\n  generate nanoid" : string.Empty)}}
            module Work
              feature Creation
                slice StateChange Create
                  command Create
                    id Id generated identifier
                    produces Created
                      for id
                    produces Initialized
                      for id
                  event Created
                  event Initialized
                  specification Creating
            {{prior}}
                    when Create
                      for "{{fixture}}"
            {{outcome}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Generation"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("generation"), "generation", "Generation.play", source);
        var compilation = new SemanticModelCompiler().Compile("Generation", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }
}

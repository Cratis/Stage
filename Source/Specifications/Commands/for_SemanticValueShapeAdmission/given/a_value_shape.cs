// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticValueShapeAdmission.given;

public class a_value_shape : Specification
{
    private protected SemanticApplicationContext _context = null!;
    protected SemanticTypeReference _type = null!;

    void Establish()
    {
        const string source = """
            concept Count : Int
              validate
                > 5
            type Node
              count Count optional
              next Node optional
              children Node[] optional
            module Inputs
              feature Checking
                slice StateChange Check
                  command CheckInput
                    value Node optional
                    returns value
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Inputs"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("inputs"), "inputs", "Inputs.play", source);
        var compilation = new SemanticModelCompiler().Compile("Inputs", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _context = new(plan);
        _type = plan.Commands.Values.Single().Properties.Single().Type;
    }
}

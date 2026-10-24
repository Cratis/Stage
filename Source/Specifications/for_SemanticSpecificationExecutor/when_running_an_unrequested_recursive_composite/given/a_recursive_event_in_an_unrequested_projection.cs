// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_unrequested_recursive_composite.given;

public class a_recursive_event_in_an_unrequested_projection : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRunRecord _result = null!;

    void Establish()
    {
        const string source = """
            type Node
              label String
              next Node optional
            module Work
              feature Items
                slice StateView Lookup
                  command Place
                    id String identifier
                    name String
                    produces Placed
                      for id
                      name = name
                  event Placed
                    name String
                  event Linked
                    node Node
                  readmodel Order
                    id String key
                    name String
                  readmodel Chain
                    id String key
                    node Node
                  projection Orders => Order
                    from Placed
                      id = $eventSourceId
                      name = name
                  projection Chains => Chain
                    from Linked
                      id = $eventSourceId
                      node = node
                  specification Checking
                    given Placed
                      for "one"
                      name = "n"
                    then readmodel Order
                      id = "one"
                      name = "n"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("UnrequestedRecursive"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("UnrequestedRecursive", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }
}

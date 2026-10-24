// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_orphan_child_events.given;

public abstract class an_orphan_child_projection : Specification
{
        protected abstract string RootOperation { get; }

    protected abstract string ChildOperation { get; }

    protected async Task<SemanticSpecificationRunRecord> Run()
    {
        var plan = when_running_property_keyed_removal_only_projections.Compile(Source);
        return Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    string Source => $$"""
        type Entry
          code String
          label String optional
        module Work
          feature Work
            slice StateView Lookup
              event Opened
                name String
              event Placed
                code String
                other String
              command RecordOpened
                source String identifier
                name String
                produces Opened
                  for source
                  name = name
              command RecordPlaced
                source String identifier
                code String
                other String
                produces Placed
                  for source
                  code = code
                  other = other
              readmodel Current
                id String key
                name String
                entries Entry[]
              projection Build => Current
                no automap
                from Opened
                  name = name
        {{RootOperation}}
                children entries identified by code
                  from Placed key code
                    parent $eventSourceId
                    code = code
                    label = other
        {{ChildOperation}}
              specification Orphan
                given Opened
                  for "other"
                  name = "other name"
                given Placed
                  for "ghost"
                  code = "one"
                  other = "other"
                then readmodel Current
                  id = "other"
                  name = "other name"
                  entries = []
        """;
}

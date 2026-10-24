// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_render_with_an_invalid_typed_context : Specification
{
    internal const string Independent = """
        
        module Independent
          feature Pings
            slice StateChange Ping
              command Ping
                id Uuid identifier
                produces Pinged
                  for id
              event Pinged
        """;

    protected LoadedSemanticModel _loaded = null!;
    protected SemanticId _owner;
    protected ArtifactRenderPlan _plan = null!;

    protected void Prepare(string source)
    {
        _loaded = opaque_policy_model.Load(source + Independent);
        var descriptor = _loaded.TypedContextDescriptors.Single();
        _owner = descriptor.OperationId.GetValueOrDefault();
        var member = descriptor.Members[0];
        _loaded = _loaded with
        {
            TypedContextDescriptors = [descriptor with
            {
                Members = descriptor.Members.SetItem(0, member with
                {
                    Type = new(SemanticContextTypeKinds.Runtime, null, null, "UnsupportedToken")
                })
            }]
        };
    }
}
#endif

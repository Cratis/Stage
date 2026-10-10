// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_specification_case_table : Specification
{
    internal const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId String identifier
                name String
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name String
              specification Registering
                parameter name String
                case Small name = "small"
                case Medium name = "medium"
                case Large name = "large"
                when RegisterProject
                  projectId = "project-1"
                  name = case.name
                then ProjectRegistered
                  name = case.name
        """;

    protected LoadedSemanticModel _loaded = null!;
    protected ArtifactRenderPlan _plan = null!;

    async Task Establish() => _loaded = await when_rendering_a_pure_reducer.Load(Source);
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.given;

public class a_composite_command_response : Specification
{
    internal const string Composite = """
        concept ProjectId : Uuid
        eventsource Project
          identifier ProjectId
        type Address
          street String
          city String
        module Projects
          feature Moving
            slice StateChange Move
              command Move
                projectId ProjectId identifier
                address Address
                produces event Moved
                  for projectId
                  address Address = address
                returns address
        """;

    protected static SemanticCommand CommandIn(string source) => SemanticVersionFeatures.Slices(invoice_model.Compile(source).Application).Single().Commands.Single();
}

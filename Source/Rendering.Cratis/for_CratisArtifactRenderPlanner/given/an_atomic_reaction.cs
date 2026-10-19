// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class an_atomic_reaction : Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation
{
    internal const string Productions = """
        module Work
          feature Processing
            slice Automation FollowUp
              event Started
                id String
              event Recorded
                note String
              event Audited
                note String
              reaction Worker
                where id != ""
                when Started
                  produces Recorded
                    for id
                    note = id
                  produces Audited
                    for "audit"
                    note = id
        """;

    internal const string Refusals = """
        module Work
          feature Processing
            slice Automation FollowUp
              event Started
                id String
              event Completed
              event Recorded
                note String
              event Audited
                note String
              command Complete
                id String identifier
                validate
                  id not empty
                produces Completed
                  for id
              reaction Worker
                runs as system
                when Started
                  invokes Complete
                    id = ""
                    on refused by validation
                      produces Recorded
                        note = $refusal.message
                      produces Audited
                        for "audit"
                        note = $refusal.reason
                    on refused
                      acknowledge
        """;

    void Establish() => SetContext(Compile("reaction batches atomic\n" + Productions));

    private protected static RenderedFile[] Files(ArtifactRenderPlan plan) => [.. plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}

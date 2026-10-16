// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class generated_local_collision_cases
{
    static readonly string[] Names = ["operation", "operation1", "occurred", "occurred1", "stageEvents", "stageEvents1", "stageProduces0", "stageProduces01", "streamIdPart0", "streamIdPart01", "source", "source1", "generatedSource0_0", "generatedSource0_01"];
    const string Pin = "11111111-1111-7111-8111-111111111111";

    internal static ExecutableSemanticModel Compile() => invoice_model.Compile($"""
        concept Receipt : Uuid
          generate uuidv7
        module Collisions
          feature Recording
        {Slice("Conditional", true)}
        {Slice("Unconditional", false)}
        """);

    static string Slice(string name, bool conditional) => $"""
            slice StateChange {name}
              command Record{name}
                id String identifier
                enabled Bool
        {string.Join('\n', Names.Select(property => $"        {property} Receipt generated"))}
        {Production(name, conditional)}
              event {name}Recorded
        {string.Join('\n', Names.Select(property => $"        value_{property} Receipt"))}
                recordedAt DateTime
        {Scenario(name, "Selected", true)}
        {Scenario(name, "Skipped", false, conditional)}
        """;

    static string Production(string name, bool conditional)
    {
        var indent = conditional ? "  " : string.Empty;
        return $"""
        {(conditional ? $"        produces when enabled == true\n          {name}Recorded" : $"        produces {name}Recorded")}
        {indent}          for id
        {string.Join('\n', Names.Select(property => $"{indent}          value_{property} = {property}"))}
        {indent}          recordedAt = $context.occurred
        """;
    }

    static string Scenario(string name, string scenario, bool enabled, bool conditional = false) => $"""
              specification {name}{scenario}
                given clock "2026-01-01T12:00:00Z"
                when Record{name}
                  id = "{Pin}"
                  enabled = {enabled.ToString().ToLowerInvariant()}
        {string.Join('\n', Names.Select(property => $"          generated {property} = \"{Pin}\""))}
        {(conditional ? "        then no events" : $"""
                then {name}Recorded
                  for "{Pin}"
        {string.Join('\n', Names.Select(property => $"          value_{property} = \"{Pin}\""))}
                  recordedAt = "2026-01-01T12:00:00Z"
        """)}
        """;
}
#endif

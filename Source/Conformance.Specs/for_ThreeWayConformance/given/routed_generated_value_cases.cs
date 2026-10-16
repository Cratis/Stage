// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class routed_generated_value_cases
{
    const string Pin = "11111111-1111-7111-8111-111111111111";
    const string Stream = "22222222-2222-4222-8222-222222222222";
    const string FreshnessError = "A generated event source already has facts; retry the command with a fresh generated value.";

    internal static ExecutableSemanticModel Compile() => invoice_model.Compile($"""
        concept Receipt : Uuid
          generate uuidv7
        concept Month : Int
        eventsource Account
          identifier Receipt
          stream UuidStream
            streamId Uuid
          stream TextStream
            streamId String
          stream CompositeStream
            streamId
              project Uuid
              month Month
              label String
        module Banking
          feature Recording
        {Slice("Uuid", "period Uuid", "UuidStream", "streamId = period", $"period = \"{Stream}\"", $"streamId = \"{Stream}\"")}
        {Slice("Text", "period String", "TextStream", "streamId = period", "period = \"a|b%\"", "streamId = \"a|b%\"")}
        {Slice("Composite", "project Uuid\n        month Month\n        label String", "CompositeStream", "streamId\n            project = project\n            month = month\n            label = label", $"project = \"{Stream}\"\n          month = 10\n          label = \"a|b%\"", $"streamId\n              project = \"{Stream}\"\n              month = 10\n              label = \"a|b%\"")}
        {Slice("Conditional", "period String", "TextStream", "streamId = period", "period = \"a|b%\"", "streamId = \"a|b%\"", true)}
        """);

    static string Slice(string name, string inputs, string stream, string route, string values, string expectedRoute, bool conditional = false) => $"""
            slice StateChange {name}
              command Record{name}
                receipt Receipt generated identifier
                {inputs}
        {(conditional ? "        enabled Bool" : string.Empty)}
                stream Account.{stream}
                  {route}
        {(conditional ? $"        produces when enabled == true\n          {name}Recorded\n            for receipt" : $"        produces {name}Recorded\n          for receipt")}
              event {name}Recorded
              specification {name}Route
                when Record{name}
                  for "{Pin}"
                  {values}
        {(conditional ? "          enabled = true" : string.Empty)}
                then {name}Recorded
                  for "{Pin}"
                  stream Account.{stream}
                    {expectedRoute}
              specification {name}CrossStreamReuse
                given {name}Recorded
                  for "{Pin}"
                  stream Account.TextStream
                    streamId = "another-stream"
                when Record{name}
                  for "{Pin}"
                  {values}
        {(conditional ? "          enabled = true" : string.Empty)}
                then error "{FreshnessError}"
        {(stream == "TextStream" ? EmptyRoute(name, conditional) : string.Empty)}
        {(conditional ? SkippedRoute(name) : string.Empty)}
        """;

    // Only the no-append outcome is shared: Arc resolves generated parameters before Handle checks the route,
    // while the reference checks the route before accepting generation fixtures.
    static string EmptyRoute(string name, bool conditional) => $"""
              specification {name}EmptyRoute
                when Record{name}
                  for "{Pin}"
                  period = ""
        {(conditional ? "          enabled = true" : string.Empty)}
                then error
        """;

    static string SkippedRoute(string name) => $"""
              specification {name}Skipped
                given {name}Recorded
                  for "{Pin}"
                  stream Account.TextStream
                    streamId = "another-stream"
                when Record{name}
                  for "{Pin}"
                  period = "a|b%"
                  enabled = false
                then no events
        """;
}
#endif

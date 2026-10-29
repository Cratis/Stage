// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

// #557: these are invocation comparisons, not replay of a prepared decision. See the matrix README.
internal static class protected_read_race_probes
{
    internal static readonly string[] InvocationVectors =
    [
        "same-event-other-key", "other-view-same-key", "no-fact-command-accept", "irrelevant-fact-preserved",
        "absent-then-created", "rejected-decision", "late-uniqueness-claim", "no-later-facts", "merged-keys"
    ];

    internal static IEnumerable<(string Name, string Source)> Files()
    {
        var vectors = JsonNode.Parse(ProtectedCommandReadRaceVectors.Bytes.AsSpan())["vectors"].AsArray();
        foreach (var name in InvocationVectors)
        {
            var vector = vectors.Single(vector => vector["name"].GetValue<string>() == name);

            // Only the accepted branch of merged-keys is an invocation comparison; the other branches replay a decision.
            var commit = vector["commits"].AsArray().Single(commit => name != "merged-keys" || commit["expect"]["outcome"].GetValue<string>() == "accepted");
            yield return ($"when_{name.Replace('-', '_')}.cs", Source(vector, commit));
        }
    }

    static string Source(JsonNode vector, JsonNode commit)
    {
        var command = vector["request"]["command"].GetValue<string>();
        var arguments = vector["request"]["values"].AsObject().Select(pair => $"{Identifiers.ToPascalCase(pair.Key)}: {Literal(pair.Value)}");
        var seed = vector["decided"].AsArray().Select(fact => $"_scenario.Given.ForEventSource({Literal(fact["source"])}).Events({Event(fact)});");
        var later = commit["later"].AsArray().Select(fact => $"_scenario.AppendConcurrently({Literal(fact["source"])}, {Event(fact)});");
        var expected = commit["expect"];
        var accepted = expected["outcome"].GetValue<string>() == "accepted";
        var facts = accepted ? expected["facts"].AsArray() : [];
        var assertions = facts.Select((fact, index) => $$"""
            [Fact] void should_append_expected_fact_{{index}}() => _scenario.AppendedEvents[{{index}}].Event.Content.ShouldEqual({{Event(fact)}});
            [Fact] void should_preserve_expected_destination_{{index}}() => _scenario.AppendedEvents[{{index}}].Event.Context.EventSourceId.Value.ShouldEqual({{Literal(fact["source"])}});
            """);
        var rejection = accepted ? string.Empty : $$"""
            [Fact] void should_preserve_the_reference_rejection_message() => _result.ValidationResults.First().Message.ShouldEqual({{Literal(expected["message"])}});
            {{(expected["category"].GetValue<string>() == "Constraint" ? $"[Fact] void should_identify_the_constraint() => _result.ShouldHaveConstraintViolationFor({Literal(expected["code"])});" : "[Fact] void should_report_validation() => _result.ValidationResults.First().Reason.ShouldEqual(Cratis.Arc.Validation.ValidationResultReason.Rule);")}}
            """;

        // A rejected Handle does not expose a prepared transaction; do not compare the later reference world for it.
        var world = accepted || expected["category"]?.GetValue<string>() == "Constraint" ? $$"""
            [Fact] async Task should_preserve_the_committed_world()
            {
                var events = await _scenario.EventSequence.GetFromSequenceNumber(Cratis.Chronicle.Events.EventSequenceNumber.First);
                events.Select(entry => entry.Content.GetType().Name).ShouldEqual(new string[] { {{string.Join(", ", expected["world"].AsArray().Select(value => Literal(value)))}} });
            }
            """ : string.Empty;

        return $$"""
            using Cratis.Arc.Chronicle.Testing.Commands;
            using Cratis.Arc.Commands;
            using Cratis.Arc.Testing.Commands;
            using Cratis.Specifications;
            using ProtectedCommandReadRaces.Races.Decisions.Write;
            using Xunit;

            namespace ProtectedCommandReadRaces.Probes;

            public class when_{{vector["name"].GetValue<string>().Replace('-', '_')}} : Specification, IDisposable
            {
                readonly CommandScenario<{{command}}> _scenario = new CommandScenario<{{command}}>().UseDecisionReads();
                CommandResult _result = null!;

                void Establish()
                {
                    {{string.Join('\n', seed)}}
                    {{string.Join('\n', later)}}
                }

                async Task Because()
                {
                    using var principalScope = new global::Cratis.Arc.Authorization.CurrentPrincipalAccessor(new global::Cratis.Arc.Http.HttpRequestContextAccessor()).BeginScope(new global::System.Security.Claims.ClaimsPrincipal(new global::System.Security.Claims.ClaimsIdentity([], "Probe")));
                    _result = await _scenario.Execute(new {{command}}({{string.Join(", ", arguments)}}));
                }

                [Fact] void should_match_the_reference_outcome() => _result.IsSuccess.ShouldEqual({{(accepted ? "true" : "false")}});
                [Fact] void should_append_only_the_expected_command_facts() => _scenario.AppendedEvents.Count.ShouldEqual({{facts.Count}});
                {{string.Join('\n', assertions)}}
                {{rejection}}
                {{world}}

                public void Dispose() => _scenario.Dispose();
            }
            """;
    }

    static string Event(JsonNode fact)
    {
        var values = fact["values"]?.AsObject().Select(pair => $"{Identifiers.ToPascalCase(pair.Key)}: {Literal(pair.Value)}") ?? [];

        return $"new {fact["event"].GetValue<string>()}({string.Join(", ", values)})";
    }

    static string Literal(JsonNode value) => CSharpCodeBuilder.StringLiteral(value.GetValue<string>());
}
#endif

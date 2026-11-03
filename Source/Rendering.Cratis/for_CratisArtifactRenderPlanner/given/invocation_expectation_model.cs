// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class invocation_expectation_model
{
    internal static ExecutableSemanticModel Create()
    {
        var golden = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(34).Bytes.AsSpan());
        var application = golden.Application with
        {
            Modules = [.. golden.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice.Specifications.IsEmpty ? slice : slice with { Specifications = Variants(slice.Specifications.Single(), feature.Slices.Single(candidate => candidate.Name == "ArchiveInvoice").Commands.Single()) })]
                })]
            })]
        };
        var local = Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(LocalSource).Application.Modules.Single();
        var feature = local.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single(candidate => candidate.Name == "Complete");
        var tags = command.Properties.Single(property => property.Name == "tags");
        var note = command.Properties.Single(property => property.Name == "note");
        var id = command.Properties.Single(property => property.Name == "id");
        var collection = SemanticValue.Array([SemanticValue.Text("one"), SemanticValue.Text("two")]);
        slice = slice with
        {
            Reactions = [.. slice.Reactions.Select(reaction => reaction with
            {
                Triggers = [.. reaction.Triggers.Select(trigger => trigger with
                {
                    Invokes = [.. trigger.Invokes.Select(invocation => invocation with { Mappings = [.. invocation.Mappings, new(note.Id, new SemanticValueExpression(SemanticValue.Null)), new(tags.Id, new SemanticValueExpression(collection))] })]
                })]
            })],
            Specifications = [.. slice.Specifications.Select(specification => specification.Name == "Refused" ? specification : specification with
            {
                ThenInvocations = [new(command.Id, [new(id.Id, (specification.WhenAppended?.Values ?? specification.When.Values).Single().Value), new(note.Id, SemanticValue.Null), new(tags.Id, collection)])]
            })]
        };
        local = local with { Features = [feature with { Slices = [slice] }] };
        return ExecutableSemanticModel.Create(golden.LanguageVersion, golden.SemanticVersion, application with { Modules = application.Modules.Add(local) });
    }

    static System.Collections.Immutable.ImmutableArray<SemanticSpecification> Variants(SemanticSpecification original, SemanticCommand wrongCommand)
    {
        var expected = original.ThenInvocations.Single();
        return [original,
            Variant("Partial", [expected with { Values = [expected.Values.Single(value => value.Value is SemanticTextValue text && text.Value == "paid")] }]),
            Variant("WrongValue", [expected with { Values = [.. expected.Values.Select(value => value.Value is SemanticTextValue text && text.Value == "paid" ? value with { Value = SemanticValue.Text("wrong") } : value)] }]),
            Variant("WrongCommand", [new(wrongCommand.Id, [])]),
            Variant("MissingInvocation", [expected, expected])];

        SemanticSpecification Variant(string name, System.Collections.Immutable.ImmutableArray<SemanticSpecificationInvocation> expectations) => original with { Id = SemanticId.Create(SemanticKind.Specification, name), Name = name, ThenInvocations = expectations };
    }

    const string LocalSource = """
        module Work
          feature Processing
            slice Automation FollowUp
              command Start
                id String identifier
                produces Started
                  for id
                  id = id
              command Complete
                id String identifier
                note String optional
                tags String[] optional
                validate
                  require id != "blocked"
                    message "blocked"
                produces Completed
                  for id
                  id = id
              event Started
                id String
              event Completed
                id String
              reaction Worker
                when Started
                  invokes Complete
                    id = id
              specification Accepted
                when append Started
                  for "one"
                  id = "one"
                then Completed
                  for "one"
                  id = "one"
              specification Refused
                when append Started
                  for "blocked"
                  id = "blocked"
                then error "blocked"
              specification Cascade
                when Start
                  id = "two"
                then Started
                  for "two"
                  id = "two"
                then Completed
                  for "two"
                  id = "two"
        """;
}
#endif

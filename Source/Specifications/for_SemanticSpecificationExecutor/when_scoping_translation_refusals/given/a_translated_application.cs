// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_scoping_translation_refusals.given;

public class a_translated_application : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRunRecord _result = null!;
    protected virtual string Scenario => "unrelated";

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Unrelated"));
        var source = """
            module Work
              feature Work
                slice Automation Work
                  command Save
                    id String identifier
                    produces Saved
                      for id
                  command Record
                    id String identifier
                    produces Historical
                      for id
                  event Saved
                  event Historical
                  event Followed
                  specification Saving
                    when Save
                      id = "one"
                    then Saved
                      for "one"
            """;
        if (Scenario == "reaction" || Scenario == "translated-reaction") source += "\n      reaction Follow\n        when Saved\n          produces Followed\n            for \"one\"";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("work"), "work", "Work.play", source);
        var compilation = new SemanticModelCompiler().Compile("Unrelated", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var work = compilation.Value!.Model.Application.Modules.Single();
        var slice = work.Features.Single().Slices.Single();
        var specification = slice.Specifications.Single();
        if (Scenario == "rejected") specification = specification with { ThenEvents = [], ThenErrors = [new(null, "Expected rejection")] };
        if (Scenario == "given")
        {
            specification = specification with
            {
                GivenEvents = [new(slice.Events.Single(@event => @event.Name == "Historical").Id, [])
                { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("one")) }]
            };
        }
        var translatedReactions = Scenario == "translated-reaction" ? slice.Reactions : [];
        slice = slice with { Specifications = Scenario == "translate" ? [] : [specification], Reactions = Scenario == "translated-reaction" ? [] : slice.Reactions };
        work = work with { Features = [work.Features.Single() with { Slices = [slice] }] };
        var original = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var publisher = feature.Slices.Single(candidate => candidate.Direction == SemanticTranslationDirection.Outbound);
        var eventName = Scenario switch { "reaction" => "Followed", "given" => "Historical", _ => "Saved" };
        var eventId = slice.Events.Single(@event => @event.Name == eventName).Id;
        publisher = publisher with
        {
            Specifications = Scenario == "translate" ? [specification] : [],
            Reactions = translatedReactions,
            Projections = Scenario == "unrelated" || Scenario == "translate" || Scenario == "translated-reaction" ? publisher.Projections :
                [.. publisher.Projections.Select(projection => projection with
                { Scope = projection.Scope! with { From = [projection.Scope!.From[0] with { EventContract = eventId, Mappings = [], Key = new SemanticProjectionValueKey(new SemanticProjectionEventSourceIdentity()) }] } })]
        };
        var application = original.Application with
        {
            Modules = [work, module with { Features = [feature with
            { Slices = [.. feature.Slices.Select(candidate => candidate.Id == publisher.Id ? publisher : candidate with { Specifications = [] })] }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application)).Plan!;
    }

    protected async Task Execute() => _result = (await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results.Single();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticRunPublication;

public class when_accumulating_publishers : Specification
{
    [Fact]
    public async Task should_suppress_a_second_publication_of_the_same_batch_content()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var application = model.Application;
        var updated = application with
        {
            Modules = [.. application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Projections = [.. slice.Projections, .. slice.Projections.Select(projection => projection with
                        {
                            Id = SemanticId.Parse("sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                            Name = "DuplicatePublisher"
                        })]
                    })]
                })]
            })]
        };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, updated)).Plan!;
        var specification = plan.Specifications.Values.Single();
        var types = new SemanticRuntimeTypes(plan);
        using var store = new EventStoreForTesting(null, new SemanticClientArtifactsProvider([.. plan.Events.Keys.Select(types.For)]));
        var context = new SemanticRunContext(typeof(DynamicCommand), null!, specification, new(), types, store.EventLog, plan);
        var appended = specification.WhenAppended!;
        await context.Append(new(appended.EventContract, appended.Values) { EventSource = appended.EventSource }, appended.EventSource?.Value ?? SemanticValue.Null, CancellationToken.None);
        (await new SemanticRunPublication(plan, context, 0).PublishPending(CancellationToken.None)).ShouldBeNull();
        context.Facts.Count.ShouldEqual(2);
        context.Facts.Count(fact => fact.EventContract == specification.ThenEvents.Single().EventContract).ShouldEqual(1);
    }
}

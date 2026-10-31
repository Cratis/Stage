// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_historical_event_used_by_a_typed_context : given.a_dependency_index
{
    void Establish()
    {
        var contract = _registerProject.Events.Single();
        var command = _registerProject.Commands.Single();
        var member = new SemanticTypedContextMember("historical", new("shape", null, contract.Id, null), false, false, new("current-event", contract.Id, "event") { EventRevision = EventContractRevision.Initial });
        var property = new SemanticContextProperty(contract.Properties[0].Name, contract.Properties[0].Id, contract.Properties[0].Type);
        var descriptor = new SemanticTypedContextDescriptor("body", SemanticImplementationRole.CommandHandler, 1, command.Id, [member])
        {
            Types = [new(contract.Id, contract.Name, SemanticTypeReferenceKind.CompositeType, SemanticPrimitiveType.Unknown, [property])]
        };
        _request = _request with { TypedContextDescriptors = [descriptor] };
    }

    void Because() => BuildIndex();

    [Fact] void should_cover_the_entire_reference_shape() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));

    [Fact] void should_record_the_historical_event_owner() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.HistoricalEvent && edge.Dependency == _registerProject.Events.Single().Id && edge.Dependent == _registerProject.Commands.Single().Id).ShouldBeTrue();
    [Fact] void should_record_shape_and_type_references() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.TypedContext && edge.Member == "SemanticContextType.Shape").ShouldBeTrue();
    [Fact] void should_resolve_context_property_identities_to_the_event_owner() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.TypedContext && edge.Member == "SemanticContextProperty.Id" && edge.Dependency == _registerProject.Events.Single().Id).ShouldBeTrue();
}

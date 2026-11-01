// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_non_root_or_ambiguous_gaps : given.an_unsupported_event_request
{
    readonly Dictionary<string, ArtifactRenderRequest> _requests = [];
    readonly Dictionary<string, ArtifactRenderPlan> _plans = [];

    void Establish()
    {
        var original = _request;
        ChangeIndependent(slice => slice with
        {
            Commands = [.. slice.Commands.Select(command => command with { Properties = Unknown(command.Properties) })]
        });
        _requests.Add("command", _request);
        _request = original;
        ChangeApplication(application => application with
        {
            Modules = [.. application.Modules.Select(module => module.Name == "Projects" ? module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        ReadModels = [.. slice.ReadModels.Select(model => model with { Properties = Unknown(model.Properties) })]
                    })]
                })]
            } : module)]
        });
        _requests.Add("read-model", _request);
        _request = original;
        ChangeApplication(application => application with
        {
            Modules = [.. application.Modules.Select(module => module.Name == "Projects" ? module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Projections = [.. slice.Projections.Select(projection => projection with { Target = (SemanticProjectionTargetKind)999 })]
                    })]
                })]
            } : module)]
        });
        _requests.Add("projection", _request);
        _request = original;
        ChangeIndependent(slice => slice with
        {
            Events = [.. slice.Events.Select(owner => owner with { Properties = Unknown(owner.Properties), Visibility = (SemanticEventVisibility)999 })]
        });
        _requests.Add("two-roots-with-visibility", _request);
        _request = original;
        ChangeApplication(application => application with
        {
            Types = [new(SemanticId.Create(SemanticKind.CompositeType, "Independent"), "Independent", [.. Unknown(_registerProject.Commands.Single().Properties).Select(property => property with { Id = SemanticId.Create(SemanticKind.Property, $"Independent.{property.Name}") })])]
        });
        _requests.Add("composite", _request);
        _request = original;
        ChangeIndependent(slice => slice with
        {
            Commands = [.. slice.Commands.Select(command => command with { Authorization = new SemanticPolicyReference("Unavailable") })]
        });
        _requests.Add("policy", _request);
        _request = original;
        ChangeIndependent(slice => slice with { Events = [.. slice.Events, _registerProject.Events.Single()] });
        _requests.Add("duplicate", _request);
    }

    void Because()
    {
        foreach (var (name, request) in _requests) _plans.Add(name, _planner.Plan(request));
    }

    [Theory]
    [InlineData("command")]
    [InlineData("read-model")]
    [InlineData("projection")]
    [InlineData("two-roots-with-visibility")]
    [InlineData("composite")]
    [InlineData("policy")]
    [InlineData("duplicate")]
    void should_not_forgive_non_root_or_ambiguous_incompleteness(string gap)
    {
        if (gap != "duplicate")
        {
            var index = new SemanticDependencyIndex(new SemanticApplicationContext(_requests[gap], _options));
            index.IsSemanticGraphComplete.ShouldBeFalse();
        }
        _plans[gap].Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
        _plans[gap].Artifacts.ShouldBeEmpty();
        _plans[gap].RequiredRemovals.ShouldBeEmpty();
        _plans[gap].Degradations.ShouldBeEmpty();
        _plans[gap].Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-012").ShouldBeTrue();
    }

    static ImmutableArray<SemanticProperty> Unknown(ImmutableArray<SemanticProperty> properties) =>
        [.. properties.Select(property => property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } })];
}

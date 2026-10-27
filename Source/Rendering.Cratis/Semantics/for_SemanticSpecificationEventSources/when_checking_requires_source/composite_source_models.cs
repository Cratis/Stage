// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

internal static class composite_source_models
{
    // The composite declarations and projection levels follow when_rendering_scoped_projections.
    const string Source = """
        type Details
          value String
        type Entry
          entryId String
          details Details?
        module Billing
          feature Entries
            slice StateView Entries
              event EntryChanged
                rootId String
                entryId String
                value String
              event DetailsChanged
                rootId String
                value String
              readmodel Root
                rootId String
                entries Entry[]
                details Details?
              query RootById => Root?
                by rootId String
              projection EntriesProjection => Root
                from EntryChanged key rootId
                  rootId = rootId
                nested details
                  from DetailsChanged key rootId
                    value = value
                children entries identified by entryId
                  from EntryChanged key entryId
                    parent rootId
                    entryId = entryId
                  nested details
                    from EntryChanged key entryId
                      parent rootId
                      value = value
              specification ChangingEntry
                given EntryChanged
                  rootId = "root-1"
                  entryId = "entry-1"
                  value = "before"
                when append EntryChanged
                  rootId = "root-1"
                  entryId = "entry-1"
                  value = "after"
                then readmodel Root
                  rootId = "root-1"
        """;

    internal static ExecutableSemanticModel Child(bool explicitParent) => Change(scope =>
    {
        var child = scope.Children.Single();
        var contribution = child.Scope.From.Single();
        return scope with
        {
            Children = [child with { Scope = child.Scope with
            {
                Nested = [],
                From = [contribution with { ParentKey = explicitParent ? SemanticProjectionKey.EventSourceIdentity : null }]
            } }]
        };
    });

    internal static ExecutableSemanticModel Nested(bool child) => Change(scope =>
    {
        if (!child)
        {
            return scope;
        }
        var collection = scope.Children.Single();
        var nested = collection.Scope.Nested.Single();
        var contribution = nested.Scope.From.Single();
        return scope with { Children = [collection with { Scope = collection.Scope with
        {
            Nested = [nested with { Scope = nested.Scope with { From = [contribution with { ParentKey = null }] } }]
        } }] };
    });

    internal static SemanticId Event(ExecutableSemanticModel model, string name) =>
        SemanticVersionFeatures.Slices(model.Application).SelectMany(slice => slice.Events).Single(contract => contract.Name == name).Id;

    static ExecutableSemanticModel Change(Func<SemanticProjectionScope, SemanticProjectionScope> change)
    {
        var model = invoice_model.Compile(Source);
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var projection = slice.Projections.Single();
        var changed = slice with { Projections = [projection with { Scope = change(projection.Scope!) }] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [changed] }] }] };
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
    }
}

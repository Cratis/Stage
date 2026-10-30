// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_root_joins_with_explicit_keys;

sealed class only_rendered_events(IReadOnlyList<Type> events) : IClientArtifactsProvider
{
    public IEnumerable<Type> EventTypes => events;
    public IEnumerable<Type> Projections => [];
    public IEnumerable<Type> ModelBoundProjections => [];
    public IEnumerable<Type> Reactors => [];
    public IEnumerable<Type> ReadModelReactors => [];
    public IEnumerable<Type> Reducers => [];
    public IEnumerable<Type> ReactorMiddlewares => [];
    public IEnumerable<Type> ComplianceForTypesProviders => [];
    public IEnumerable<Type> ComplianceForPropertiesProviders => [];
    public IEnumerable<Type> AdditionalEventInformationProviders => [];
    public IEnumerable<Type> ConstraintTypes => [];
    public IEnumerable<Type> UniqueConstraints => [];
    public IEnumerable<Type> UniqueEventTypeConstraints => [];
    public IEnumerable<Type> RemoveConstraintEventTypes => [];
    public IEnumerable<Type> EventTypeMigrators => [];
    public IEnumerable<Type> EventSeeders => [];
}

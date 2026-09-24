// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Stage.Specifications.Types;

/// <summary>
/// Restricts Chronicle's in-process event store to the event contracts of this semantic run.
/// </summary>
/// <param name="eventTypes">Runtime-emitted event contract types.</param>
public sealed class SemanticClientArtifactsProvider(IReadOnlyList<Type> eventTypes) : IClientArtifactsProvider
{
    /// <inheritdoc/>
    public IEnumerable<Type> EventTypes => eventTypes;

    /// <inheritdoc/>
    public IEnumerable<Type> Projections => [];

    /// <inheritdoc/>
    public IEnumerable<Type> ModelBoundProjections => [];

    /// <inheritdoc/>
    public IEnumerable<Type> Reactors => [];

    /// <inheritdoc/>
    public IEnumerable<Type> ReadModelReactors => [];

    /// <inheritdoc/>
    public IEnumerable<Type> Reducers => [];

    /// <inheritdoc/>
    public IEnumerable<Type> ReactorMiddlewares => [];

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForTypesProviders => [];

    /// <inheritdoc/>
    public IEnumerable<Type> ComplianceForPropertiesProviders => [];

    /// <inheritdoc/>
    public IEnumerable<Type> AdditionalEventInformationProviders => [];

    /// <inheritdoc/>
    public IEnumerable<Type> ConstraintTypes => [];

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueConstraints => [];

    /// <inheritdoc/>
    public IEnumerable<Type> UniqueEventTypeConstraints => [];

    /// <inheritdoc/>
    public IEnumerable<Type> RemoveConstraintEventTypes => [];

    /// <inheritdoc/>
    public IEnumerable<Type> EventTypeMigrators => [];

    /// <inheritdoc/>
    public IEnumerable<Type> EventSeeders => [];
}

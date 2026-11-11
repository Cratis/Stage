// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.DependencyInjection;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Finds a performer among several owners, the first that has one.
/// </summary>
/// <param name="owners">The owners, asked in order.</param>
[IgnoreConvention]
internal sealed class ComposedWorkflowPerformers(params IWorkflowNodePerformers[] owners) : IWorkflowNodePerformers
{
    /// <inheritdoc/>
    public IWorkflowNodePerformer? For(CapabilityReference capability) =>
        owners.Select(owner => owner.For(capability)).FirstOrDefault(performer => performer is not null);
}

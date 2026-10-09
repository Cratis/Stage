// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_canceling_source_planning : given.a_source_plan
{
    Exception? _error;
    async Task Because() => _error = await Catch.Exception(async () => await CratisRendering.PlanFrom(new PlaySources(_root, []), _featureSelection, _planOptions, new CancellationToken(canceled: true)));
    [Fact] void should_propagate_cancellation() => _error.ShouldBeOfExactType<OperationCanceledException>();
}

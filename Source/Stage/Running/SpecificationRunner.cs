// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Running;

/// <summary>
/// Default <see cref="ISpecificationRunner"/> that walks the model and runs each specification through the
/// strategy registered for its slice type.
/// </summary>
public sealed class SpecificationRunner : ISpecificationRunner
{
    readonly Dictionary<SliceType, ISpecificationRunStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationRunner"/> class with the default strategies.
    /// </summary>
    public SpecificationRunner()
        : this(new StateChangeRunStrategy(), new StateViewRunStrategy(), new ReactorRunStrategy())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificationRunner"/> class.
    /// </summary>
    /// <param name="stateChange">The strategy for state change slices.</param>
    /// <param name="stateView">The strategy for state view slices.</param>
    /// <param name="reactor">The strategy for automation and translator slices.</param>
    public SpecificationRunner(
        ISpecificationRunStrategy stateChange,
        ISpecificationRunStrategy stateView,
        ISpecificationRunStrategy reactor)
    {
        _strategies = new Dictionary<SliceType, ISpecificationRunStrategy>
        {
            [SliceType.StateChange] = stateChange,
            [SliceType.StateView] = stateView,
            [SliceType.Automation] = reactor,
            [SliceType.Translator] = reactor,
        };
    }

    /// <inheritdoc/>
    public SpecificationRunResults Run(EventModel model, Guid? sliceId = null, Guid? specificationId = null)
    {
        var results = new List<SpecificationRunResult>();

        foreach (var located in StageModelWalker.Slices(model))
        {
            var slice = located.Slice;
            if (sliceId is { } targetSlice && slice.Id != targetSlice)
            {
                continue;
            }

            if (!_strategies.TryGetValue(slice.SliceType, out var strategy))
            {
                continue;
            }

            foreach (var specification in slice.Specifications ?? [])
            {
                if (specificationId is { } targetSpecification && specification.Id != targetSpecification)
                {
                    continue;
                }

                results.Add(strategy.Run(slice, specification));
            }
        }

        return new SpecificationRunResults(model.Id, results);
    }
}

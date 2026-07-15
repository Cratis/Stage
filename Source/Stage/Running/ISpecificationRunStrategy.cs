// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Running;

/// <summary>
/// Runs a single specification against a slice of a particular <see cref="SliceType"/>.
/// </summary>
public interface ISpecificationRunStrategy
{
    /// <summary>
    /// Gets the slice type this strategy handles.
    /// </summary>
    SliceType SliceType { get; }

    /// <summary>
    /// Runs the specification against its slice.
    /// </summary>
    /// <param name="slice">The slice the specification belongs to.</param>
    /// <param name="specification">The specification to run.</param>
    /// <returns>The <see cref="SpecificationRunResult"/> for the specification.</returns>
    SpecificationRunResult Run(Slice slice, Specification specification);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications;

namespace Cratis.Stage.Running;

/// <summary>
/// Runs the given/when/then specifications modeled on the slices of an event model and reports the outcome.
/// </summary>
/// <remarks>
/// The default implementation verifies a specification structurally — that it refers to artifacts that exist
/// on its slice and that the modeled command rules are consistent with the modeled Then errors. Behavioral
/// execution against a live Chronicle is intended to be a drop-in implementation behind this same interface.
/// </remarks>
public interface ISpecificationRunner
{
    /// <summary>
    /// Runs the specifications in the model, optionally filtered to a single slice and/or specification.
    /// </summary>
    /// <param name="model">The event model whose specifications to run.</param>
    /// <param name="sliceId">When set, only run specifications on the slice with this identifier.</param>
    /// <param name="specificationId">When set, only run the specification with this identifier.</param>
    /// <returns>The <see cref="SpecificationRunResults"/> for every specification that was run.</returns>
    SpecificationRunResults Run(EventModel model, Guid? sliceId = null, Guid? specificationId = null);
}

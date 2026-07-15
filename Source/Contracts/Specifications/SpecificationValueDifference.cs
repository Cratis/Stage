// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Specifications;

/// <summary>
/// Represents a single value mismatch between the expected (modeled) and actual outcome of a run.
/// </summary>
/// <param name="Path">The property path the mismatch was found at (for example <c>Name</c> or <c>Address.City</c>).</param>
/// <param name="Expected">The expected value as modeled in the specification, or <see langword="null"/> when none was expected.</param>
/// <param name="Actual">The actual value produced by the run, or <see langword="null"/> when none was produced.</param>
public record SpecificationValueDifference(string Path, string? Expected, string? Actual);

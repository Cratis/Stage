// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Conformance.Specs.for_RenderOutcomeContract;

/// <summary>
/// An exact, reviewed code/reason disposition supplied by the invoking conformance surface.
/// Never generate these from observed errors; corpus migration owns its reviewed dispositions.
/// </summary>
/// <param name="Code">The complete diagnostic code, not a namespace prefix.</param>
/// <param name="Message">The complete diagnostic reason, matched ordinally.</param>
/// <param name="Category">The safety boundary being protected.</param>
public sealed record RenderOutcomeSafetyCase(string Code, string Message, RenderOutcomeSafetyCategory Category);

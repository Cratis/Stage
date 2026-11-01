// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Aggregate plan limits. All values must be positive.</summary>
public sealed record TemplateBudget(long MaxSteps, long MaxOutputBytes, int MaxArtifacts, int MaxFanOut, int MaxStringLength, int MaxRecursion, TimeSpan Deadline)
{
    /// <summary>Gets the v1 default limits.</summary>
    public static TemplateBudget Default { get; } = new(2_000_000, 8 * 1024 * 1024, 2000, 2000, 256 * 1024, 64, TimeSpan.FromSeconds(10));
}

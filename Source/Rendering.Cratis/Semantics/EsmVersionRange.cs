// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Represents an inclusive semantic version floor and an exclusive major ceiling.
/// </summary>
internal sealed record EsmVersionRange
{
    internal EsmVersionRange(SemanticVersion minimum, uint exclusiveMajor)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(minimum, default);
        if ((ulong)minimum.Major + 1 != exclusiveMajor)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMajor), "The range must have a nondefault minimum and end at the next major.");
        }
        Minimum = minimum;
        ExclusiveMajor = exclusiveMajor;
    }

    internal SemanticVersion Minimum { get; }
    internal uint ExclusiveMajor { get; }

    public override string ToString() => $">={Minimum} <{ExclusiveMajor}";

    internal bool Contains(SemanticVersion version) => version.IsAtLeast(Minimum) && version.Major < ExclusiveMajor;
}

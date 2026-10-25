// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static partial class GeneratedSpecificationResults
{
    [GeneratedRegex(@"Total:\s*(?<total>\d+), Errors:\s*(?<errors>\d+), Failed:\s*(?<failed>\d+), Skipped:\s*(?<skipped>\d+), Not Run:\s*(?<notRun>\d+)", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Summary { get; }
    public static void ShouldHavePassedSpecifications(this string output, int? expectedTotal = null) => Verify(output, expectedTotal, 0);

    public static void ShouldHaveFailedSpecifications(this string output, int expectedFailures) => Verify(output, null, expectedFailures);

    static void Verify(string output, int? expectedTotal, int expectedFailures)
    {
        var summary = Assert.Single(Summary.Matches(output).Cast<Match>());
        var counts = summary.Groups.Cast<Group>().Skip(1).Select(group => int.Parse(group.Value, CultureInfo.InvariantCulture)).ToArray();
        Assert.True(counts[0] > 0, output);
        if (expectedTotal is not null) Assert.Equal(expectedTotal.Value, counts[0]);
        Assert.Equal(0, counts[1]);
        Assert.Equal(expectedFailures, counts[2]);
        Assert.True(counts[0] >= counts[2], output);
        Assert.Equal(0, counts[3]);
        Assert.Equal(0, counts[4]);
    }
}
#endif

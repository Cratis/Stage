// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Holds the emitted C# to the standard a reviewer would hold hand-written code to.
/// </summary>
/// <remarks>
/// Generated code lands in someone's repository and is read, reviewed and built there, frequently under
/// stricter analyser settings than this repository uses. A using nobody uses is a warning in that build and a
/// change request in that review, and the author cannot fix it by editing the file.
/// </remarks>
public partial class when_planning_generated_code_quality : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_plan_without_errors() => _plan.Diagnostics.ShouldBeEmpty();

    /// <summary>
    /// The globalization namespace is only needed by a value that renders as a culture-invariant parse.
    /// </summary>
    [Fact] void should_not_declare_the_globalization_namespace_without_using_it() =>
        Sources()
            .Any(_ => _.Text.Contains("using System.Globalization;", StringComparison.Ordinal) && !_.Text.Contains("CultureInfo", StringComparison.Ordinal))
            .ShouldBeFalse();

    [Fact] void should_not_declare_a_namespace_twice_in_one_file() =>
        Sources()
            .Any(_ => UsingDirective.Matches(_.Text)
                .Select(match => match.Groups["namespace"].Value)
                .GroupBy(namespaceName => namespaceName, StringComparer.Ordinal)
                .Any(group => group.Count() > 1))
            .ShouldBeFalse();

    [Fact] void should_order_usings_so_a_style_analyser_does_not_reject_them() =>
        Sources()
            .Any(_ =>
            {
                var declared = UsingDirective.Matches(_.Text)
                    .Select(match => match.Groups["namespace"].Value)
                    .ToArray();
                return !declared.SequenceEqual(declared.Order(StringComparer.Ordinal));
            })
            .ShouldBeFalse();

    [Fact] void should_leave_no_blank_line_at_the_end_of_a_file() =>
        Sources().Any(_ => _.Text.EndsWith("\n\n", StringComparison.Ordinal)).ShouldBeFalse();

    [GeneratedRegex("(?m)^using (?<namespace>[A-Za-z0-9_.]+);", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UsingDirective { get; }

    IEnumerable<(string Path, string Text)> Sources() =>
        _plan.Artifacts
            .Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .Select(_ => (_.RelativePath, Text(_)));
}

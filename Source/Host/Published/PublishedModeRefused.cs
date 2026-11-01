// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Published;

/// <summary>
/// Thrown when a published host is asked to start without what publishing requires.
/// </summary>
internal sealed class PublishedModeRefused : Exception
{
    internal PublishedModeRefused(IReadOnlyList<string> problems)
        : base(Format(problems))
    {
        Problems = problems;
    }

    internal IReadOnlyList<string> Problems { get; }

    static string Format(IReadOnlyList<string> problems) =>
        $"Stage cannot start in published mode:{Environment.NewLine}{string.Join(Environment.NewLine, problems.Select(problem => $"  - {problem}"))}";
}

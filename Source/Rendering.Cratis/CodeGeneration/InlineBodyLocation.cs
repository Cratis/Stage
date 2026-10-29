// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Where an authored body sits inside generated code, with the means to map an offset inside the spliced text back to the authored body.
/// </summary>
/// <param name="Start">The offset in the generated code where the spliced body starts.</param>
/// <param name="Presented">The text as it was spliced, without the terminating line break.</param>
/// <param name="Body">The authored body.</param>
internal sealed record InlineBodyLocation(int Start, string Presented, string Body)
{
    /// <summary>
    /// Gets the length of the spliced text.
    /// </summary>
    internal int Length => Presented.Length;

    /// <summary>
    /// Maps an offset inside the spliced text to the offset in the authored body.
    /// </summary>
    /// <param name="presentedOffset">The offset relative to <see cref="Start"/>.</param>
    /// <returns>The offset in <see cref="Body"/>.</returns>
    internal int ToBody(int presentedOffset)
    {
        if (ReferenceEquals(Presented, Body) || string.Equals(Presented, Body, StringComparison.Ordinal)) return Math.Clamp(presentedOffset, 0, Body.Length);
        var offset = Math.Clamp(presentedOffset, 0, Presented.Length);
        var presented = CSharpInlineIndentation.Lines(Presented);
        var original = CSharpInlineIndentation.Lines(Body);
        if (presented.Count != original.Count) return Math.Clamp(offset, 0, Body.Length);

        var index = presented.FindLastIndex(line => line.Start <= offset);
        var (presentedLine, originalLine) = (presented[Math.Max(0, index)], original[Math.Max(0, index)]);
        var column = offset - presentedLine.Start;
        if (presentedLine.Content == 0) return originalLine.Start;

        // A re-indented line differs from the authored one only by its leading whitespace, so the length difference is the shift.
        var shifted = column - (presentedLine.Content - originalLine.Content);
        return originalLine.Start + Math.Clamp(shifted, 0, originalLine.Content + originalLine.Break);
    }
}

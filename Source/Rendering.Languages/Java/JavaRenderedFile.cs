// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Holds complete provisional output and lossless segment provenance through Java Unicode translation.
/// </summary>
public sealed class JavaRenderedFile
{
    readonly int[] _origins;
    readonly int[] _starts;
    readonly string[] _fragments;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaRenderedFile"/> class.
    /// </summary>
    /// <param name="file">The originating symbol file.</param>
    /// <param name="fragments">One emitted fragment for each segment, before optional shortening.</param>
    public JavaRenderedFile(SymbolFile file, IEnumerable<string> fragments)
    {
        File = file;
        _fragments = [.. fragments];
        Text = string.Concat(_fragments);
        _starts = new int[_fragments.Length];
        _origins = new int[Text.Length];
        var position = 0;
        for (var index = 0; index < _fragments.Length; index++)
        {
            _starts[index] = position;
            Array.Fill(_origins, index, position, _fragments[index].Length);
            position += _fragments[index].Length;
        }

        JavaOutputIntegrity.Validate(Text);
        Tokens = JavaSourceLexer.Scan(Text);
    }

    /// <summary>Gets the originating structured file.</summary>
    public SymbolFile File { get; }

    /// <summary>Gets the complete provisional emitted text.</summary>
    public string Text { get; }

    /// <summary>Gets translated tokens mapped to raw emitted intervals.</summary>
    public IReadOnlyList<JavaSourceToken> Tokens { get; }

    /// <summary>Gets a segment's raw start.</summary>
    /// <param name="segment">The zero-based segment identity.</param>
    /// <returns>The raw UTF-16 start.</returns>
    public int Start(int segment) => _starts[segment];

    /// <summary>Determines whether a token is precisely a whole segment, rather than a mixed or merged name.</summary>
    /// <param name="token">The translated token.</param>
    /// <param name="segment">The segment identity.</param>
    /// <returns>Whether both raw boundaries match.</returns>
    public bool IsWholeSegment(JavaSourceToken token, int segment) => token.Start == _starts[segment] && token.End == _starts[segment] + _fragments[segment].Length;

    /// <summary>Finds all originating segments, including provenance splits inside a single translated token.</summary>
    /// <param name="token">The translated token.</param>
    /// <returns>The contributing segment identities.</returns>
    public IReadOnlyList<int> Origins(JavaSourceToken token) => [.. _origins.AsSpan(token.Start, token.End - token.Start).ToArray().Distinct()];

    /// <summary>Determines whether every character of a token originates in native spans.</summary>
    /// <param name="token">The translated token.</param>
    /// <returns>Whether no generated segment contributes to it.</returns>
    public bool IsNative(JavaSourceToken token) => Origins(token).All(index => File.Segments[index] is NativeSegment);

    /// <summary>
    /// Emits final replacements, normalizing only raw CR/CRLF in generated text outside literals.
    /// Authored native spans and literal/text-block contents are copied byte-for-byte.
    /// </summary>
    /// <param name="replace">Returns a replacement for each non-text segment.</param>
    /// <returns>The final output text.</returns>
    public string Finish(Func<int, string> replace) => string.Concat(File.Segments.Select((segment, index) => segment is TextSegment ? Normalize(index) : replace(index)));

    string Normalize(int segment)
    {
        var fragment = _fragments[segment];
        var literal = Tokens.Where(token => token.Literal && token.Start < _starts[segment] + fragment.Length && token.End > _starts[segment]).ToArray();
        var output = new StringBuilder(fragment.Length);
        for (var index = 0; index < fragment.Length; index++)
        {
            var position = _starts[segment] + index;
            if (fragment[index] != '\r' || literal.Any(token => token.Start <= position && position < token.End)) { output.Append(fragment[index]); continue; }
            output.Append('\n');
            if (index + 1 < fragment.Length && fragment[index + 1] == '\n') index++;
        }

        return output.ToString();
    }
}

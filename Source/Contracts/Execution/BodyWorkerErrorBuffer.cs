// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Retains bounded diagnostic text while the child error stream continues draining.</summary>
/// <param name="maximumBytes">The maximum number of UTF-8 bytes retained.</param>
internal sealed class BodyWorkerErrorBuffer(int maximumBytes)
{
    readonly StringBuilder _text = new();
    int _bytes;

    public override string ToString() => _text.ToString();

    internal void Append(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            var count = Encoding.UTF8.GetByteCount(new ReadOnlySpan<char>(in character));
            if (_bytes + count > maximumBytes) continue;
            _text.Append(character);
            _bytes += count;
        }
    }
}

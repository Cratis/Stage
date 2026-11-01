// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Bounded line transport; never buffers an unbounded peer response.</summary>
public static class BodyWorkerPipe
{
    /// <summary>Reads a bounded response, rejecting EOF, truncation and oversized lines.</summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="maxBytes">The maximum UTF-8 byte length of the line.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <exception cref="IOException">The stream ended, the line was truncated, or it exceeded the limit.</exception>
    public static async Task<string> ReadLineAsync(TextReader reader, int maxBytes, CancellationToken cancellationToken) =>
        await TryReadLineAsync(reader, maxBytes, cancellationToken).ConfigureAwait(false) ?? throw new IOException("Worker exited before completing its response.");

    /// <summary>Reads one UTF-8-size-bounded line, returning null only for clean EOF.</summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="maxBytes">The maximum UTF-8 byte length of the line.</param>
    /// <param name="cancellationToken">The token that cancels the read.</param>
    /// <exception cref="IOException">The line was truncated or exceeded the limit.</exception>
    public static async Task<string?> TryReadLineAsync(TextReader reader, int maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new char[1];
        var line = new StringBuilder();
        var bytes = 0;
        while (await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false) != 0)
        {
            if (buffer[0] == '\n') return line.ToString();
            bytes += Encoding.UTF8.GetByteCount(buffer);
            if (bytes > maxBytes) throw new IOException("Worker line exceeded its byte limit.");
            line.Append(buffer[0]);
        }
        return line.Length == 0 ? null : throw new IOException("Worker line was truncated.");
    }
}

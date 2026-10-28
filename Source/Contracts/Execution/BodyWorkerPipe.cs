// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Bounded line transport; never buffers an unbounded child response.</summary>
public static class BodyWorkerPipe
{
    /// <summary>Reads one UTF-8-size-bounded line, rejecting truncated and oversized responses.</summary>
    public static async Task<string> ReadLineAsync(TextReader reader, int maxBytes, CancellationToken cancellationToken)
    {
        var buffer = new char[1];
        var line = new StringBuilder();
        var bytes = 0;
        while (await reader.ReadAsync(buffer.AsMemory(), cancellationToken) != 0)
        {
            if (buffer[0] == '\n') return line.ToString();
            bytes += Encoding.UTF8.GetByteCount(buffer);
            if (bytes > maxBytes) throw new IOException("Worker response exceeded its byte limit.");
            line.Append(buffer[0]);
        }
        throw new IOException("Worker exited before completing its response.");
    }
}

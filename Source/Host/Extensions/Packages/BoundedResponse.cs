// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Reads a response into memory, refusing anything larger than the deployment allows even when the server does not say how large it is.
/// </summary>
public static class BoundedResponse
{
    /// <summary>
    /// Reads the body of a response.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="maxBytes">The most bytes to accept.</param>
    /// <param name="cancellationToken">Cancelled when loading stops.</param>
    /// <returns>The bytes.</returns>
    /// <exception cref="PackageRefusedException">Thrown when the body is larger than <paramref name="maxBytes"/>.</exception>
    public static async Task<byte[]> Read(HttpResponseMessage response, long maxBytes, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > maxBytes) throw TooLarge();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes) throw TooLarge();
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }

    static PackageRefusedException TooLarge() => new(PackageRefusalReason.TooLarge, "The registry's answer is larger than the deployment allows.");
}

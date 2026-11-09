// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// Waits for <c language="text">/stage/status</c> to leave the transient <c language="text">loading</c> state,
/// so a caller can wait for a settled session with one request instead of polling.
/// </summary>
internal static class StageStatusWait
{
    /// <summary>
    /// The longest wait, in seconds, a caller can ask for.
    /// </summary>
    internal const int MaximumSeconds = 300;

    /// <summary>
    /// The state that is still settling; every other state is reported immediately.
    /// </summary>
    internal const string Loading = "loading";

    /// <summary>
    /// How often the status is read again while it is loading.
    /// </summary>
    internal static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Reads the status and, while it is loading, reads it again until it changes or the wait elapses.
    /// </summary>
    /// <param name="status">Reads the current status.</param>
    /// <param name="waitSeconds">How long to wait, in seconds; <see langword="null"/> or zero reads once. Clamped to 0..<see cref="MaximumSeconds"/>.</param>
    /// <param name="delay">Waits between reads.</param>
    /// <param name="cancellationToken">Stops waiting, for example when the request is aborted.</param>
    /// <returns>The last status read.</returns>
    internal static async Task<StageStatus> Until(Func<StageStatus> status, int? waitSeconds, Func<TimeSpan, CancellationToken, Task> delay, CancellationToken cancellationToken)
    {
        var current = status();
        var remaining = TimeSpan.FromSeconds(Math.Clamp(waitSeconds ?? 0, 0, MaximumSeconds));
        while (current.State == Loading && remaining > TimeSpan.Zero && !cancellationToken.IsCancellationRequested)
        {
            var step = remaining < Interval ? remaining : Interval;
            try
            {
                await delay(step, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return current;
            }

            remaining -= step;
            current = status();
        }

        return current;
    }
}

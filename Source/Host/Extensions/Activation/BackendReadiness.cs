// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Finds a port to give a backend and waits for the backend to listen on it.
/// </summary>
public static class BackendReadiness
{
    /// <summary>
    /// Finds a free port on an address. The port is free when this returns; the backend takes it as it starts.
    /// </summary>
    /// <param name="address">The address the backend will listen on.</param>
    /// <returns>The port.</returns>
    public static int FreePort(IPAddress address)
    {
        using var listener = new TcpListener(address, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    /// <summary>
    /// Waits until something accepts connections on an address, the backend is gone or the time is up.
    /// </summary>
    /// <param name="address">The address the backend listens on.</param>
    /// <param name="exited">Completes, with the reason, when the backend is gone.</param>
    /// <param name="timeout">How long the backend may take.</param>
    /// <param name="cancellationToken">Cancelled when the caller stops waiting.</param>
    /// <returns>A task that completes when the backend accepts connections.</returns>
    /// <exception cref="BackendStartException">The backend exited or took too long.</exception>
    public static async Task WaitUntilListening(Uri address, Task<string> exited, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(timeout);
        while (true)
        {
            if (exited.IsCompleted) throw new BackendStartException($"The backend exited before it listened on {address}: {await exited}");
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(address.Host, address.Port, limit.Token);
                return;
            }
            catch (SocketException)
            {
                // Nothing listens yet.
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new BackendStartException($"The backend did not listen on {address} within {timeout}.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), limit.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new BackendStartException($"The backend did not listen on {address} within {timeout}.");
            }
        }
    }
}

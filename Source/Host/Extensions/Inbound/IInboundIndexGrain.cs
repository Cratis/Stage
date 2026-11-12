// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// The list of stored inbound requests the dispatcher and the Workbench read. There is one, with the key <see cref="Key"/>.
/// </summary>
[Alias("Cratis.Stage.Host.Extensions.Inbound.IInboundIndexGrain")]
public interface IInboundIndexGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the key of the one index grain.
    /// </summary>
    public const string Key = "default";

    /// <summary>
    /// Lists a request as open. Listing one that is listed changes nothing.
    /// </summary>
    /// <param name="id">The identity.</param>
    /// <returns>Awaitable task.</returns>
    [Alias("Open")]
    Task Open(string id);

    /// <summary>
    /// Takes a request off the open list.
    /// </summary>
    /// <param name="id">The identity.</param>
    /// <returns>Awaitable task.</returns>
    [Alias("Close")]
    Task Close(string id);

    /// <summary>
    /// Lists the open requests.
    /// </summary>
    /// <returns>The identities.</returns>
    [Alias("ListOpen")]
    Task<IReadOnlyList<string>> ListOpen();

    /// <summary>
    /// Lists the requests, newest first.
    /// </summary>
    /// <param name="limit">The most to list.</param>
    /// <returns>The identities.</returns>
    [Alias("Recent")]
    Task<IReadOnlyList<string>> Recent(int limit);
}

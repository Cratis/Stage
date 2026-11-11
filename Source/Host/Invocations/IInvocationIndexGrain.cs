// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The list of invocations the Workbench shows. There is one, with the key <see cref="Key"/>.
/// </summary>
[Alias("Cratis.Stage.Host.Invocations.IInvocationIndexGrain")]
public interface IInvocationIndexGrain : IGrainWithStringKey
{
    /// <summary>
    /// Gets the key of the one index grain.
    /// </summary>
    public const string Key = "default";

    /// <summary>
    /// Adds an invocation. Adding one that is listed changes nothing.
    /// </summary>
    /// <param name="identity">The invocation identity.</param>
    /// <returns>Awaitable task.</returns>
    [Alias("Add")]
    Task Add(string identity);

    /// <summary>
    /// Lists the invocations, newest first.
    /// </summary>
    /// <param name="limit">The most to list.</param>
    /// <returns>The identities.</returns>
    [Alias("List")]
    Task<IReadOnlyList<string>> List(int limit);
}

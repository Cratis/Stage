// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications;

/// <summary>
/// Describes why the executor refused to accept a specification's action.
/// </summary>
/// <param name="Message">The rejection message.</param>
/// <param name="Code">The optional rejection code, such as a violated constraint name.</param>
/// <param name="Denied">Whether the caller was denied rather than the input rejected.</param>
internal sealed record SemanticRejection(string Message, string? Code = null, bool Denied = false)
{
    /// <summary>
    /// Gets the rejection for a caller that is not authorized.
    /// </summary>
    internal static SemanticRejection Unauthorized { get; } = new("Caller is not authorized.", Denied: true);
}

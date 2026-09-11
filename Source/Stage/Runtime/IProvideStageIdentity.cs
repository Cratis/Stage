// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// Defines the system that resolves the identity behind the current command, for event properties the model sources
/// from <c language="csharp">$context.identity</c> or <c language="csharp">$causedBy</c>.
/// </summary>
public interface IProvideStageIdentity
{
    /// <summary>
    /// Gets the identity values available for the current command, keyed by the path the model refers to them by
    /// (<c language="csharp">id</c>, <c language="csharp">name</c>, <c language="csharp">userName</c>, <c language="csharp">subject</c>).
    /// </summary>
    /// <returns>The identity values; empty when there is no authenticated caller.</returns>
    IReadOnlyDictionary<string, string> Current();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay form's <c>populate</c> declaration is a subtype of
/// <c>FormPopulateSource</c> that <see cref="FormConverter"/> does not yet know how to convert.
/// </summary>
/// <param name="typeName">The name of the unrecognized <c>FormPopulateSource</c> subtype.</param>
public class UnknownFormPopulateSource(string typeName) : Exception($"'{typeName}' is not a known form populate source");

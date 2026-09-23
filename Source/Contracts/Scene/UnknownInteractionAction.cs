// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay interaction action is a subtype of
/// <c language="csharp">InteractionActionSyntax</c> that <see cref="InteractionActionConverter"/> does not yet know how to convert.
/// </summary>
/// <param name="typeName">The name of the unrecognized <c language="csharp">InteractionActionSyntax</c> subtype.</param>
public class UnknownInteractionAction(string typeName) : Exception($"'{typeName}' is not a known interaction action");

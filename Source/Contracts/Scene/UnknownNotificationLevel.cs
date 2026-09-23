// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a <c language="csharp">notify</c> action carries a level that
/// <see cref="InteractionActionConverter"/> does not know how to convert.
/// </summary>
/// <param name="level">The unrecognized level.</param>
public class UnknownNotificationLevel(string level) : Exception($"'{level}' is not a known notification level");

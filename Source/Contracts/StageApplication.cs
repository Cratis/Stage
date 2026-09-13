// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Contracts;

/// <summary>
/// The executable event model and renderable Scene translated from one compiled Screenplay application.
/// </summary>
/// <param name="EventModel">The event model Stage registers and performs.</param>
/// <param name="Scene">The platform-neutral UI model a frontend renders.</param>
public sealed record StageApplication(EventModel EventModel, SceneApplication Scene);

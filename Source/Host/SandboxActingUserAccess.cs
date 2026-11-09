// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>The original host boundary, retained before Stage informs Arc of its development environment.</summary>
/// <param name="Mode">The host's immutable run mode.</param>
/// <param name="Production">Whether the process started in Production.</param>
/// <param name="Development">Whether Stage is in development.</param>
internal sealed record SandboxActingUserAccess(StageHostMode Mode, bool Production, bool Development);

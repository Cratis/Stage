// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// The exception that is thrown when the requested host mode cannot safely start.
/// </summary>
/// <param name="reason">The configuration requirement that was not met.</param>
internal sealed class StageHostModeRefused(string reason) : Exception(reason);

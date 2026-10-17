// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

internal static partial class StageRuntimeEngineLogging
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "The {Engine} engine is deprecated and will be removed in Stage v6. Use the semantic engine instead.")]
    internal static partial void Deprecated(ILogger logger, string engine);
}

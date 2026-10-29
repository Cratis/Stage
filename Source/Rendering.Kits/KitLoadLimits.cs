// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Bounds kit materialization before parsing or rendering.</summary>
/// <param name="MaxFiles">Maximum number of files.</param>
/// <param name="MaxFileBytes">Maximum bytes in one file.</param>
/// <param name="MaxTotalBytes">Maximum bytes across all files.</param>
public sealed record KitLoadLimits(int MaxFiles = 2048, long MaxFileBytes = 8 * 1024 * 1024, long MaxTotalBytes = 64 * 1024 * 1024);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

sealed class KitSourceException(string code, string path) : IOException($"Unsafe kit source entry: {path}.")
{
    internal string Code { get; } = code;

    internal string EntryPath { get; } = path;
}

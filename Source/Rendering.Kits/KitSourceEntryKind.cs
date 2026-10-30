// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>The filesystem-independent kind of a kit entry.</summary>
public enum KitSourceEntryKind
{
    File,
    Directory,
    SymbolicLink,
    Other
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Sensitivity;

/// <summary>
/// The kind of position a <see cref="SensitivityNode"/> describes.
/// </summary>
internal enum SensitivityKind
{
    /// <summary>An ordinary value.</summary>
    Leaf,

    /// <summary>A secret.</summary>
    Secret,

    /// <summary>A position whose content cannot be seen into.</summary>
    Opaque,

    /// <summary>A graph that could not be read.</summary>
    Unreadable,

    /// <summary>A record, nested type or command with named members.</summary>
    Composite,

    /// <summary>A list of elements.</summary>
    Collection
}

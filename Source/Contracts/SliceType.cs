// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts;

/// <summary>
/// Defines the type of a slice within a feature. Mirrors the authoring model's slice types.
/// </summary>
public enum SliceType
{
    /// <summary>A slice that accepts a command and changes state by appending events.</summary>
    StateChange,

    /// <summary>A slice that projects events into a queryable read model.</summary>
    StateView,

    /// <summary>A slice that reacts to events and performs side effects.</summary>
    Automation,

    /// <summary>A slice that reacts to events and appends follow-up events to another stream.</summary>
    Translator,
}

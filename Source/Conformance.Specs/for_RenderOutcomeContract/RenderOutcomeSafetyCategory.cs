// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Conformance.Specs.for_RenderOutcomeContract;

/// <summary>
/// Safety boundaries admitted by the outcome contract, independently of diagnostic naming.
/// </summary>
public enum RenderOutcomeSafetyCategory
{
    /// <summary>Untrusted or unaudited input.</summary>
    Trust,

    /// <summary>Malformed contracts, scopes, paths or identities.</summary>
    Integrity,

    /// <summary>An exceeded execution or resource limit.</summary>
    Budget,

    /// <summary>A caller or policy boundary that cannot safely be preserved.</summary>
    Authorization,

    /// <summary>An event destination or routing boundary that cannot safely be preserved.</summary>
    UnsafeEventRouting
}

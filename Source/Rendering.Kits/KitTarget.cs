// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>The generic target identifier and exact target version.</summary>
public sealed record KitTarget(
    [property: KitPattern("^[a-z][a-z0-9-]*$")] string Id,
    [property: KitPattern("^" + LanguagePackVersion.SemverPattern + "$")] string Version);

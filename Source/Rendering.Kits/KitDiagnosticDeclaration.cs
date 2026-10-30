// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>A kit-owned diagnostic, without an assigned severity.</summary>
public sealed record KitDiagnosticDeclaration(
    [property: KitPattern("^[A-Z][A-Z0-9]*(-[A-Z0-9]+)+$")] string Code,
    string Summary);

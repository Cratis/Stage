// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// Describes a versioned helper and its frozen input/output examples.
/// </summary>
/// <param name="Name">Qualified helper name.</param>
/// <param name="Signature">Target-neutral calling signature.</param>
/// <param name="Summary">Helper behavior and restrictions.</param>
/// <param name="Since">First template profile version providing the helper.</param>
/// <param name="Vectors">Frozen examples, rendered with the test language pack.</param>
public sealed record HelperDescriptor(string Name, string Signature, string Summary, string Since = "v1", ImmutableArray<HelperVector> Vectors = default);

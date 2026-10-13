// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Describes a destination event identity that publication would change or cannot determine safely.
/// </summary>
/// <param name="Path">The application-root-relative artifact path.</param>
/// <param name="TypeName">The namespace-qualified type name.</param>
/// <param name="ExistingId">The existing stored identity, or its unreadable expression.</param>
/// <param name="PlannedId">The planned stored identity.</param>
public sealed record CratisEventTypeIdChange(string Path, string TypeName, string ExistingId, string? PlannedId);

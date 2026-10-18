// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Holds the converted model and the constructs omitted from it.
/// </summary>
/// <param name="Model">The converted model.</param>
/// <param name="Diagnostics">The omissions, in reporting order.</param>
public sealed record EventModelConversion(EventModel Model, IReadOnlyList<ConversionDiagnostic> Diagnostics);

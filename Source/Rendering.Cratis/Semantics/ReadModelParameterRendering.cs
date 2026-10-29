// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Describes the input types and optional projection attributes for read model parameters.
/// </summary>
/// <param name="ReducerInput">Whether parameters accept reducer input types.</param>
/// <param name="Attributes">The attribute prefix for each parameter, when supplied.</param>
internal sealed record ReadModelParameterRendering(bool ReducerInput, Func<SemanticProperty, string>? Attributes = null);

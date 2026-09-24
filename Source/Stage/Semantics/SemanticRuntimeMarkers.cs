// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Semantics;

/// <summary>
/// Names the scoped HTTP markers used when Arc redacts an unsupported semantic outcome.
/// </summary>
public static class SemanticRuntimeMarkers
{
    /// <summary>
    /// Gets the request-item key carrying the safe, typed unsupported outcome.
    /// </summary>
    public const string UnsupportedMessage = "Stage.Semantic.UnsupportedMessage";

    internal const string Unsupported = "Stage.Semantic.Unsupported";
}

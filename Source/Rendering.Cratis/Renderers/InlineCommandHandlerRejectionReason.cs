// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Renderers;

/// <summary>
/// Why a legacy inline handler could not be proven independent of the generated context parameter.
/// </summary>
public enum InlineCommandHandlerRejectionReason
{
    /// <summary>
    /// Only C# bodies are supported.
    /// </summary>
    UnsupportedLanguage,

    /// <summary>
    /// Directives can change the body across build configurations.
    /// </summary>
    Directives,

    /// <summary>
    /// The complete text is not a syntactically valid method body.
    /// </summary>
    MalformedBody,

    /// <summary>
    /// A reference or candidate binds to the generated context parameter.
    /// </summary>
    ContextBinding,

    /// <summary>
    /// A context identifier cannot be conclusively bound elsewhere.
    /// </summary>
    UncertainContextBinding,

    /// <summary>
    /// Symbol analysis could not be completed.
    /// </summary>
    AnalysisFailure,
}

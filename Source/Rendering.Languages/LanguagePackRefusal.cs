// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages;

/// <summary>
/// Represents an integrity refusal by a language pack, not a semantic degradation.
/// </summary>
/// <param name="code">The pack's catalogued refusal code.</param>
/// <param name="message">The reason for refusing the input.</param>
public sealed class LanguagePackRefusal(string code, string message) : Exception(message)
{
    /// <summary>
    /// Gets the pack's catalogued refusal code.
    /// </summary>
    public string Code { get; } = code;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// The language pack cannot safely express an input.
/// </summary>
/// <param name="message">Reason.</param>
public sealed class TemplateLanguageRefusal(string message) : Exception(message);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// The exception that is thrown when an installed template package does not contain the expected template.
/// </summary>
/// <param name="shortName">The short name of the missing template.</param>
public class TemplateNotFound(string shortName)
    : Exception($"The template package was installed, but no template with the short name '{shortName}' was found in it.");

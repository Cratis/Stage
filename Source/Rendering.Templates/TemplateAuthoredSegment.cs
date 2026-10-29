// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// A structured handle to an authored implementation body. The body text is never part of the template output.
/// </summary>
/// <param name="RequirementId">The implementation requirement whose body the caller substitutes byte for byte.</param>
public sealed record TemplateAuthoredSegment(string RequirementId) : TemplateSegment;

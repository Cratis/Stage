// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>The target-owned surface. Returned text must be deterministic, bounded and fully escaped.</summary>
public interface ITemplateLanguage
{
    /// <summary>Gets the target identifier.</summary>
    string LanguageId { get; }

    /// <summary>Emits a complete string literal.</summary>
    string EscapeString(string value);

    /// <summary>Emits an integer literal.</summary>
    string FormatInteger(long value);

    /// <summary>Emits a decimal literal.</summary>
    string FormatDecimal(decimal value);

    /// <summary>Emits a Boolean literal.</summary>
    string FormatBoolean(bool value);

    /// <summary>Emits a null literal.</summary>
    string FormatNull();

    /// <summary>Emits safe comment text.</summary>
    string EscapeComment(string value);

    /// <summary>Emits an escaped target identifier.</summary>
    string EscapeIdentifier(string value);

    /// <summary>Renders a validated semantic type, or returns null if unsupported.</summary>
    TemplateSegment[]? RenderType(TemplateType type);

    /// <summary>Formats deterministic imports for a symbol set.</summary>
    string FormatImports(IReadOnlyList<SymbolDescriptor> symbols);

    /// <summary>Emits an honest placeholder in the specified role.</summary>
    TemplateSegment[] EmitGap(TemplateGap gap);
}

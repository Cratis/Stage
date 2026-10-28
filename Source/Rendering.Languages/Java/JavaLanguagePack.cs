// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Kits;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// The prerelease java@1 language pack.
/// </summary>
/// <param name="facts">Host-supplied inheritable-name facts for external types, merged with the bundled JDK facts.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "The accepted template v1 language seam names literal operations by type.")]
public sealed class JavaLanguagePack(JavaTypeFacts? facts = null) : IDeclarativeLanguage
{
    static readonly FrozenDictionary<string, TemplateSymbol> _primitives = new Dictionary<string, TemplateSymbol>(StringComparer.Ordinal)
    {
        ["text"] = new("java.lang", "String"),
        ["wholeNumber"] = new("java.lang", "Long"),
        ["decimalNumber"] = new("java.math", "BigDecimal"),
        ["boolean"] = new("java.lang", "Boolean"),
        ["uuid"] = new("java.util", "UUID"),
        ["date"] = new("java.time", "LocalDate"),
        ["dateTime"] = new("java.time", "OffsetDateTime")
    }.ToFrozenDictionary(StringComparer.Ordinal);

    readonly JavaTypeFacts _facts = facts is null ? JavaTypeFacts.Bundled : JavaTypeFacts.Bundled.Union(facts);

    /// <inheritdoc/>
    public LanguagePackVersion Pack { get; } = new("java", "1.0.0-prerelease");

    /// <inheritdoc/>
    public string String(string value) => JavaLiterals.EscapeString(value);

    /// <inheritdoc/>
    public string Integer(long value) => value.ToString(CultureInfo.InvariantCulture) + "L";

    /// <inheritdoc/>
    public string Decimal(decimal value) => "new java.math.BigDecimal(" + JavaLiterals.EscapeString(value.ToString(CultureInfo.InvariantCulture)) + ")";

    /// <inheritdoc/>
    public string Boolean(bool value) => value ? "true" : "false";

    /// <inheritdoc/>
    public string Null() => "null";

    /// <inheritdoc/>
    public string Comment(string value) => JavaLiterals.EscapeComment(value);

    /// <inheritdoc/>
    public string Identifier(string value) => JavaIdentifiers.Sanitize(value);

    /// <inheritdoc/>
    public TypeSpelling Primitive(string primitive) =>
        _primitives.TryGetValue(primitive, out var name)
            ? new([new TemplateSymbolSegment(TemplateSymbolKind.Reference, name)])
            : throw new TemplateLanguageRefusal($"{JavaRefusalCodes.UnsupportedPrimitive}: The primitive '{primitive}' has no Java type.");

    /// <inheritdoc/>
    public TypeSpelling Collection(TypeSpelling element) =>
        new([new TemplateSymbolSegment(TemplateSymbolKind.Reference, new("java.util", "List")), new TemplateTextSegment("<"), .. element.Parts, new TemplateTextSegment(">")]);

    /// <inheritdoc/>
    public TypeSpelling Optional(TypeSpelling element) => element;

    /// <inheritdoc/>
    public string Placeholder(GapRole role, string code, string intent)
    {
        var message = code + ": " + intent;
        return role is GapRole.ThrowingOperationBody or GapRole.DenyEnclosingOperation
            ? "throw new java.lang.UnsupportedOperationException(" + JavaLiterals.EscapeString(message) + ");"
            : "/* TODO " + JavaLiterals.EscapeComment(message) + " */";
    }

    /// <inheritdoc/>
    public bool TryGet(string semanticId, out TemplateSymbol symbol)
    {
        symbol = null!;
        return false;
    }

    /// <inheritdoc/>
    public ImmutableArray<DeclarativeFileResolution> Resolve(ImmutableArray<DeclarativeLanguageFile> files) =>
        [.. ResolveWithSites(files).Select(file => file.Resolution)];

    /// <inheritdoc/>
    public AuthoredAdmission Admit(string requirementId, string body)
    {
        try
        {
            PrivateUseCodePoints.Validate(body);
            return AuthoredAdmission.Admitted;
        }
        catch (LanguagePackRefusal refusal)
        {
            return AuthoredAdmission.Refused(JavaRefusalCodes.AuthoredIntegrity, refusal.Message);
        }
    }

    /// <summary>Resolves files and observes each structured reference's emitted span and intended type.</summary>
    /// <param name="files">The complete plan files.</param>
    internal ImmutableArray<JavaResolvedFile> ResolveWithSites(ImmutableArray<DeclarativeLanguageFile> files)
    {
        try
        {
            return JavaResolution.ResolveWithSites(JavaPlan.Create(files), _facts);
        }
        catch (LanguagePackRefusal refusal)
        {
            return [.. files.Select(_ => new JavaResolvedFile(DeclarativeFileResolution.Refused(refusal.Code, refusal.Message), []))];
        }
    }
}

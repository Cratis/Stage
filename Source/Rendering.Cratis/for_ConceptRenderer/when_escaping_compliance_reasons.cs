// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_escaping_compliance_reasons : concepts
{
    const string Reason = "Quoted \"note\" in C:\\notes\r\nNext line\u0085Next\u2028Line\u2029Paragraph – æ";
    RenderedFile[] _files = [];
    Assembly _assembly = null!;

    void Because()
    {
        _files = [.. new[] { "pii", "sensitive" }.Select(marker => ConceptRenderer.Render(
            new ConceptSyntax(marker == "pii" ? "PersonalNote" : "SecretNote", "String", [new ConceptAttributeSyntax(marker, SourceLocation.Start, Reason)], [], SourceLocation.Start),
            _applicationSet,
            "Generated",
            complianceDetails: true))];
        _assembly = RenderedOutput.Load(_files);
    }

    [Fact] void should_escape_quotes_and_backslashes() => _files.All(file => file.Content.Contains("Quoted \\\"note\\\" in C:\\\\notes", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_escape_line_breaks_and_unicode_separators() => _files.All(file => file.Content.Contains("\\u000D\\u000ANext line\\u0085Next\\u2028Line\\u2029Paragraph", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_recover_the_exact_personal_reason() => _assembly.GetType("Generated.Common.PersonalNote")!.GetCustomAttribute<ComplianceDetailsAttribute>()!.Details.ShouldEqual(Reason);
    [Fact] void should_recover_the_exact_secret_reason() => _assembly.GetType("Generated.Common.SecretNote")!.GetCustomAttribute<EncryptedAttribute>()!.Details.ShouldEqual(Reason);
    [Fact] void should_use_the_chronicle_default_scope() => _assembly.GetType("Generated.Common.SecretNote")!.GetCustomAttribute<EncryptedAttribute>()!.Scope.ShouldEqual(new EncryptedAttribute().Scope);
}

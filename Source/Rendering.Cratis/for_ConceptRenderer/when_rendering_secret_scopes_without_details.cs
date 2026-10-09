// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_secret_scopes_without_details : compliance_concepts
{
    Dictionary<string, RenderedFile> _files = null!;
    bool _explicitOffMatchesDefault;
    bool _absentReasonsPreserveScopes;
    RenderedFile _bothWithScope = null!;

    void Because()
    {
        _files = _applicationSet.Concepts.ToDictionary(
            entry => entry.Key,
            entry => ConceptRenderer.Render(entry.Value, _applicationSet, "Generated"));
        _explicitOffMatchesDefault = _applicationSet.Concepts.All(entry =>
            ConceptRenderer.Render(entry.Value, _applicationSet, "Generated", complianceDetails: false).Content == _files[entry.Key].Content);
        _absentReasonsPreserveScopes = new[] { "NamespaceSecret", "GlobalSecret" }.All(name =>
        {
            var concept = _applicationSet.Concepts[name];
            var withoutReason = concept with
            {
                Attributes = [.. concept.Attributes.Select(attribute => attribute with { Reason = null })]
            };
            return ConceptRenderer.Render(withoutReason, _applicationSet, "Generated").Content == _files[name].Content;
        });
        var both = _applicationSet.Concepts["PersonalSecret"];
        _bothWithScope = ConceptRenderer.Render(both with
        {
            Attributes = [.. both.Attributes.Select(attribute => attribute.Name == ConceptAttributeSyntax.Sensitive ? attribute with { Scope = "global" } : attribute)]
        },
        _applicationSet,
        "Generated");
    }

    [Fact] void should_render_namespace_scope_without_reason() => _files["NamespaceSecret"].Content.ShouldContain("[Encrypted(EncryptionScope.Namespace)]\n[NotAudited]");
    [Fact] void should_render_global_scope_without_reason() => _files["GlobalSecret"].Content.ShouldContain("[Encrypted(EncryptionScope.Global)]\n[NotAudited]");
    [Fact] void should_omit_namespace_reason() => _files["NamespaceSecret"].Content.ShouldNotContain("Shared credential.");
    [Fact] void should_omit_global_reason() => _files["GlobalSecret"].Content.ShouldNotContain("Installation credential.");
    [Fact] void should_render_the_same_scope_without_an_authored_reason() => _absentReasonsPreserveScopes.ShouldBeTrue();
    [Fact] void should_render_the_same_output_with_explicitly_disabled_details() => _explicitOffMatchesDefault.ShouldBeTrue();
    [Fact] void should_keep_subject_scope_bare() => _files["SubjectSecret"].Content.ShouldContain("[Encrypted]\n[NotAudited]");
    [Fact] void should_keep_unspecified_scope_bare() => _files["BareSecret"].Content.ShouldContain("[Encrypted]\n[NotAudited]");
    [Fact] void should_keep_reason_only_secret_bare() => _files["ReasonSecret"].Content.ShouldContain("[Encrypted]\n[NotAudited]");
    [Fact] void should_keep_personal_data_marked_pii() => _bothWithScope.Content.ShouldContain("[PII]");
    [Fact] void should_ignore_secret_scope_on_personal_data() => _bothWithScope.Content.ShouldEqual(_files["PersonalSecret"].Content);
    [Fact] void should_not_encrypt_personal_data_as_a_secret() => _bothWithScope.Content.ShouldNotContain("Encrypted");
    [Fact] void should_resolve_scope_only_attributes_against_the_pinned_client() => RenderedOutput.Errors(_files.Values).ShouldBeEmpty();
}

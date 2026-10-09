// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_compliance_details : compliance_concepts
{
    Dictionary<string, RenderedFile> _files = null!;
    RenderedFile _bothWithScope = null!;

    void Because()
    {
        _files = _applicationSet.Concepts.ToDictionary(
            entry => entry.Key,
            entry => ConceptRenderer.Render(entry.Value, _applicationSet, "Generated", complianceDetails: true));
        var both = _applicationSet.Concepts["PersonalSecret"];
        _bothWithScope = ConceptRenderer.Render(both with
        {
            Attributes = [.. both.Attributes.Select(attribute => attribute.Name == "sensitive" ? attribute with { Scope = "global" } : attribute)]
        },
        _applicationSet,
        "Generated",
        complianceDetails: true);
    }

    [Fact] void should_keep_bare_personal_data_without_details() => _files["BarePersonal"].Content.ShouldNotContain("ComplianceDetails");
    [Fact] void should_render_the_personal_alias_reason() => _files["ReasonPersonal"].Content.ShouldContain("[ComplianceDetails(\"A contact address.\")]");
    [Fact] void should_render_every_human_category() => new Dictionary<string, string>
    {
        ["Origin"] = "racial or ethnic origin",
        ["Opinions"] = "political opinions",
        ["Beliefs"] = "religious or philosophical beliefs",
        ["Membership"] = "trade union membership",
        ["Genetic"] = "genetic",
        ["Biometric"] = "biometric",
        ["Health"] = "health",
        ["Sexuality"] = "sex life or sexual orientation"
    }.All(entry => _files[entry.Key].Content.Contains($"[ComplianceDetails(\"GDPR Art. 9(1) special category: {entry.Value}.\")]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_render_criminal_data() => _files["Convictions"].Content.ShouldContain("[ComplianceDetails(\"GDPR Art. 10 criminal offence data.\")]");
    [Fact] void should_order_special_then_criminal_then_reason() => _files["MedicalNote"].Content.ShouldContain("[ComplianceDetails(\"GDPR Art. 9(1) special category: health. GDPR Art. 10 criminal offence data. Clinical evidence.\")]");
    [Fact] void should_keep_pii_for_every_personal_concept() => _applicationSet.Concepts.Where(entry => entry.Value.AttributeNames.Contains("pii")).All(entry => _files[entry.Key].Content.Contains("[PII]", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_only_render_personal_details_for_both_markers() => _files["PersonalSecret"].Content.ShouldContain("[PII]\n[ComplianceDetails(\"Personal credential.\")]");
    [Fact] void should_not_encrypt_both_markers() => _files["PersonalSecret"].Content.ShouldNotContain("Encrypted");
    [Fact] void should_not_render_the_secret_reason_for_both_markers() => _files["PersonalSecret"].Content.ShouldNotContain("Do not expose.");
    [Fact] void should_ignore_explicit_secret_scope_on_personal_data() => _bothWithScope.Content.ShouldEqual(_files["PersonalSecret"].Content);
    [Fact] void should_not_add_not_audited_for_both_markers() => _files["PersonalSecret"].Content.ShouldNotContain("NotAudited");
    [Fact] void should_keep_bare_secret_output() => _files["BareSecret"].Content.ShouldContain("[Encrypted]\n[NotAudited]");
    [Fact] void should_use_chronicle_default_scope_for_a_reason_without_scope() => _files["ReasonSecret"].Content.ShouldContain("[Encrypted(EncryptionScope.Subject, \"A credential.\")]\n[NotAudited]");
    [Fact] void should_render_explicit_subject_without_a_reason() => _files["SubjectSecret"].Content.ShouldContain("[Encrypted(EncryptionScope.Subject, \"\")]\n[NotAudited]");
    [Fact] void should_render_namespace_scope_and_reason() => _files["NamespaceSecret"].Content.ShouldContain("[Encrypted(EncryptionScope.Namespace, \"Shared credential.\")]\n[NotAudited]");
    [Fact] void should_render_global_scope_and_reason() => _files["GlobalSecret"].Content.ShouldContain("[Encrypted(EncryptionScope.Global, \"Installation credential.\")]\n[NotAudited]");
    [Fact] void should_resolve_all_attributes_against_the_pinned_client() => RenderedOutput.Errors(_files.Values).ShouldBeEmpty();
}

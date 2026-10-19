// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_a_generation_rule_command_and_requirements : Specification
{
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        var corpus = GenerationFixturesCorpus.V21;
        var form = corpus.SourceForms.Single(form => form.Name == "single");
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text)).ToArray();
        var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compiled.Success, string.Join(Environment.NewLine, compiled.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        var requirements = compiled.ImplementationRequirements;
        _admission = new(plan, id => requirements.FirstOrDefault(requirement => requirement.RequirementId == id)?.Member);
    }

    [Fact] void should_name_the_rule_and_the_concept() => _admission.Blocking.Single().Details.ShouldEqual("STAGE-ESM-028: Command 'TenantValue' generates 'value' with 'generate rule TenantOrder' on concept 'TenantOrderId'; the rule has an implementation body, which Stage's interpreter never runs or replaces with a generated value — render the application.");
}

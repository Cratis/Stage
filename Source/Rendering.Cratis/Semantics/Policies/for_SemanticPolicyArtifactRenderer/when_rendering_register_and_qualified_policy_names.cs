// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyArtifactRenderer;

public class when_rendering_register_and_qualified_policy_names : for_SemanticPolicyNames.given.a_policy_application
{
    RenderedFile[] _files = [];
    IReadOnlyList<string> _errors = [];

    void Establish()
    {
        _model = invoice_model.Compile(Policies + "\npolicy Register\n  require role \"Staff\"\n" +
            Module("Billing", Feature("Invoicing", Slice("IssueInvoice", "Register"), Slice("VoidInvoice", "Owner"), Slice("CancelInvoice", "Owner", "orderId"))));
    }

    void Because()
    {
        CratisRenderingOptions options = new("InvoiceApp", "Invoices");
        var context = new SemanticApplicationContext(
            new(
                _model,
                SemanticExecutionPlan.Compile(_model).Plan!,
                CratisRendering.CreateProfile(_model.Application.Name, options),
                new(ArtifactRenderScopeKind.Application, _model.Application.Id)),
            options);
        _files = [.. SemanticPolicyArtifactRenderer.Render(context, context.AllSlices), new("Registration.cs", """
            namespace Invoices.GeneratedPolicies;
            public static partial class Registration
            {
                static partial void RegisterGenerated(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services);
            }
            """)];
        _errors = RenderedOutput.Errors(_files);
    }

    [Fact] void should_compile_the_generated_policies_without_initializer_type_collisions() => string.Join(Environment.NewLine, _errors).ShouldEqual(string.Empty);
    [Fact] void should_keep_the_register_policy_class_name() => ClassOf("IssueInvoice").ShouldEqual("RegisterPolicy");
    [Fact] void should_keep_the_qualified_policy_class_names() => (ClassOf("VoidInvoice"), ClassOf("CancelInvoice")).ShouldEqual(("OwnerForVoidInvoicePolicy", "OwnerForCancelInvoicePolicy"));
    [Fact] void should_register_the_modeled_name() => _files.Single(file => file.RelativePath.EndsWith("RegisterPolicy.cs", StringComparison.Ordinal)).Content.ShouldContain("Registration.Add(\"Register\",");
    [Fact] void should_keep_initializers_outside_the_policy_type_naming_scheme() => _files.Where(file => file.RelativePath.EndsWith("Policy.cs", StringComparison.Ordinal)).All(file => file.Content.Contains("internal static void InitializeGeneratedAuthorization()", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif

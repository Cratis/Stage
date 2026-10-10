// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication.given;

public class a_policy_plan : Specification
{
    protected LoadedSemanticModel _loaded = null!;
    protected ArtifactRenderPlan _application = null!;
    protected ArtifactRenderPlan _plan = null!;
    protected Dictionary<string, string> _existing = [];
    protected List<string> _readPaths = [];
    protected CratisPublicationCheck _check = null!;

    // The aggregate registration and policy declaration emitted at 270a4c3^, with a fixture operation identity.
    protected const string LegacyPolicies = """
        namespace Invoices.GeneratedPolicies;
        public static partial class Registration
        {
            static partial void RegisterGenerated(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            {
                services.AddArcAuthorizationPolicy<StagePolicy_issue>("StagePolicy_issue");
            }
        }
        public sealed class StagePolicy_issue : global::Cratis.Arc.Authorization.IAuthorizationPolicy
        {
            public global::System.Threading.Tasks.ValueTask<bool> IsAuthorized(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, global::System.Threading.CancellationToken cancellationToken)
                => global::System.Threading.Tasks.ValueTask.FromResult(context.Principal.Identity?.IsAuthenticated == true);
        }
        """;

    void Establish()
    {
        _loaded = opaque_policy_model.Load(opaque_policy_model.Source("return context.Identity.IsAuthenticated;"));
        _application = opaque_policy_model.Plan(_loaded);
        _plan = CratisRendering.PlanFrom(_loaded, new([PlanSelectionEntry.Slice("Billing", "Invoicing", "Issue")]), new(_loaded.Model.Application.Name, "InvoiceApp", "Invoices")).Plan!;
    }

    protected string? Read(string path)
    {
        _readPaths.Add(path);

        return _existing.GetValueOrDefault(path);
    }

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
#endif

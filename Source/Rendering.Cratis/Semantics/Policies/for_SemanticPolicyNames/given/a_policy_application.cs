// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames.given;

/// <summary>
/// Builds small applications whose commands bind modeled policies and names their generated policies.
/// </summary>
public class a_policy_application : Specification
{
    protected const string Policies = """
        concept InvoiceId : String
        policy Staff
          require role "Staff"
        policy Admin
          require role "Admin"
        policy Owner
          require claim "owner" matches subject
        """;

    protected ExecutableSemanticModel _model = null!;
    protected ArtifactRenderPlan _plan = null!;

    // One state-change slice with a single command whose identifier carries the given name.
    protected static string Slice(string name, string authorize, string identifier = "invoiceId", string? eventName = null) => $$"""
            slice StateChange {{name}}
              command {{name}}
                authorize {{authorize}}
                {{identifier}} InvoiceId identifier
                produces {{eventName ?? name + "Done"}}
                  for {{identifier}}
                  {{identifier}} = {{identifier}}
              event {{eventName ?? name + "Done"}}
                {{identifier}} InvoiceId
        """;

    protected static string Feature(string name, params string[] slices) => $$"""
          feature {{name}}
        {{string.Join('\n', slices)}}
        """;

    protected static string Module(string name, params string[] features) => $$"""
        module {{name}}
        {{string.Join('\n', features)}}
        """;

    protected ExecutableSemanticModel Compile(params string[] modules) =>
        _model = invoice_model.Compile(Policies + "\n" + string.Join('\n', modules));

    protected SemanticId CommandId(string name) =>
        _model.Application.Modules.SelectMany(module => module.Features).SelectMany(Flatten).SelectMany(slice => slice.Commands).Single(command => command.Name == name).Id;

    protected string ClassOf(string command) => policy_names.ClassOf(_model, CommandId(command));

    protected string NameOf(string command) => policy_names.NameOf(_model, CommandId(command));

    protected ArtifactRenderPlan Plan(ArtifactRenderScopeKind kind, SemanticId id) => invoice_model.Plan(_model, new(kind, id));

    protected static string Text(ArtifactRenderPlan plan, string path) =>
        Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());

    static IEnumerable<SemanticSlice> Flatten(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Flatten));
}
#endif

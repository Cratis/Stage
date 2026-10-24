// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Finds the generated policy of a protected operation of an application model.
/// </summary>
public static class policy_names
{
    /// <summary>
    /// Gets the class name of the policy that guards an operation.
    /// </summary>
    /// <param name="model">The application model.</param>
    /// <param name="operation">The protected command or query.</param>
    /// <returns>The policy class name.</returns>
    public static string ClassOf(ExecutableSemanticModel model, SemanticId operation) => SemanticPolicyNames.For(Context(model))[operation].ClassName;

    /// <summary>
    /// Gets the name the policy of an operation is registered with.
    /// </summary>
    /// <param name="model">The application model.</param>
    /// <param name="operation">The protected command or query.</param>
    /// <returns>The registered policy name.</returns>
    public static string NameOf(ExecutableSemanticModel model, SemanticId operation) => SemanticPolicyNames.PolicyNameOf(Context(model), operation);

    static SemanticApplicationContext Context(ExecutableSemanticModel model)
    {
        CratisRenderingOptions options = new("InvoiceApp", "Invoices");
        return new(new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)), options);
    }
}

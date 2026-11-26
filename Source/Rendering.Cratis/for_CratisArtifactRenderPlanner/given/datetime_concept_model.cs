// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using FluentValidation;
using FluentValidation.Results;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class datetime_concept_model
{
    internal static ArtifactRenderPlan Plan(string source)
    {
        var model = invoice_model.Compile(source);
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var plan = CratisRendering.Plan(model, execution.Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("DateConcepts", "DateConcepts"));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        return plan;
    }

    internal static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web).ConfigureArcDefaults();

    internal static RenderedFile[] Files(ArtifactRenderPlan plan) => member_collision_model.Files(plan);

    internal static Assembly Load(string source) => RenderedOutput.Load(Files(Plan(source)));

    internal static Type Command(Assembly assembly) => assembly.GetTypes().Single(type => type.Name == "Keep");

    internal static object Bind(Assembly assembly, string json) => JsonSerializer.Deserialize(json, Command(assembly), Options)!;

    internal static ValidationResult Validate(Assembly assembly, object command)
    {
        var validator = (IValidator)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == "KeepValidator"));
        var validation = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(command.GetType()), command);
        return validator.Validate(validation);
    }

    internal const string OptionalSource = """
        concept Moment : DateTime
        module Dates
          feature Windows
            slice StateChange Keep
              command Keep
                id String identifier
                at Moment optional
                validate
                  at >= "2026-02-01T00:00:00Z" message "Command minimum."
                returns id
        """;

    internal static string Source(string target = "Moment") => $$"""
        concept Moment : DateTime
        concept RecordedMoment : DateTime
        module Dates
          feature Windows
            slice StateChange Keep
              command Keep
                id String identifier
                from Moment
                to Moment
                validate
                  require to >= from
                    message "Window reversed."
                produces Kept
                  for id
                  at = to
                returns to
              event Kept
                at {{target}}
        """;
}
#endif

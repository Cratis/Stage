// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public static class a_plan_at_an_esm_pair
{
    public static SemanticExecutionPlan From(uint major, uint minor, Func<SemanticApplication, SemanticApplication>? change = null)
    {
        var source = compiled_plan.From("module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered");
        var model = ExecutableSemanticModel.Create(source.Model.LanguageVersion, source.Model.SemanticVersion, source.Model.Application);
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, change?.Invoke(model.Application) ?? model.Application);
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion(major, minor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion(major, minor));
        return SemanticExecutionPlan.Compile(model).Plan!;
    }
}

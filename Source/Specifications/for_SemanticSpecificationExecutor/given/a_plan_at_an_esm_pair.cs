// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_plan_at_an_esm_pair : a_command_only_plan
{
    protected static void Versions(ExecutableSemanticModel model, uint major, uint minor)
    {
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion(major, minor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion(major, minor));
    }

    protected SemanticExecutionPlan WithApplication(Func<SemanticApplication, SemanticApplication> change, uint major, uint minor)
    {
        var model = ExecutableSemanticModel.Create(_plan.Model.LanguageVersion, _plan.Model.SemanticVersion, _plan.Model.Application);
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, change(model.Application));
        Versions(model, major, minor);
        return SemanticExecutionPlan.Compile(model).Plan!;
    }
}

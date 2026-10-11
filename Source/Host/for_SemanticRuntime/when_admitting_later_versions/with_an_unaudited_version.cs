// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_an_unaudited_version : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        _plan = compiled_plan.From("module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered");

        // Simulate a future dependency admitting a version Stage has not audited.
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new LanguageVersion(10, 0));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new SemanticVersion(10, 0));
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_the_whole_model() => _admission.Blocking.Single().Kind.ShouldEqual("application");
    [Fact] void should_report_the_stage_version_gate() => _admission.Blocking.Single().Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal).ShouldBeTrue();
}

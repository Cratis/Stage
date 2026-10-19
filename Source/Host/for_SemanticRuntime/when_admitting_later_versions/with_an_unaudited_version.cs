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
    SemanticRuntimeAdmission[] _admissions = [];

    void Establish()
    {
        _plan = compiled_plan.From("module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered");
    }

    void Because()
    {
        // Simulate dependency versions Stage has not audited.
        _admissions = [.. new uint[] { 31, 32 }.Select(version =>
        {
            typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new LanguageVersion(version, 0));
            typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_plan.Model, new SemanticVersion(version, 0));
            return new SemanticRuntimeAdmission(_plan);
        })];
    }

    [Fact] void should_refuse_the_whole_model() => _admissions.ShouldEachConformTo(admission => admission.Blocking.Single().Kind == "application");
    [Fact] void should_report_the_stage_version_gate() => _admissions.ShouldEachConformTo(admission => admission.Blocking.Single().Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal));
}

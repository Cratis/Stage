// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_generated_values_and_responses : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId generated identifier
                name String
                validate
                  name not empty
                  require name != ""
                produces event ProjectRegistered
                  name String = name
                returns projectId
            slice StateChange RenameProject
              command RenameProject
                projectId ProjectId identifier
                name String
                produces event ProjectRenamed
                  for projectId
                  name String = name
                returns name
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_compile_an_esm_v7_model() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_admit_generated_values_and_responses_with_request_rules_and_requirements() => _admission.Blocking.ShouldBeEmpty();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_scoped_translation_refusals : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        var original = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var unrelated = specification_plan.Create().Model.Application.Modules.Single();
        var application = original.Application with { Modules = [.. original.Application.Modules, unrelated] };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application)).Plan!;
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_admit_the_unrelated_specification() => _admission.Entries.Single(entry => entry.Kind == "specification" && entry.Artifact == _plan.Specifications.Values.Single(specification => specification.Name == "RegisteringAProject").Id.ToString()).Status.ShouldEqual("supported");
    [Fact] void should_admit_supported_translation_specifications() => _admission.Entries.Where(entry => entry.Kind == "specification" && entry.Artifact != _plan.Specifications.Values.Single(specification => specification.Name == "RegisteringAProject").Id.ToString()).All(entry => entry.Status == "supported").ShouldBeTrue();
}

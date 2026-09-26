// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_auditing_runtime_admission : a_semantic_runtime
{
    SemanticRuntimeAdmission _admission = null!;

    void Because() => _admission = new(_runtime.Plan);

    [Fact] void should_not_claim_to_register_chronicle_projection_mirrors() => _admission.Entries.Any(entry => entry.Kind == "projectionMirror").ShouldBeFalse();
}

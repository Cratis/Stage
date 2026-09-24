// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_auditing_projection_mirrors : a_semantic_runtime
{
    SemanticRuntimeAdmission _admission = null!;

    void Because() => _admission = new(_runtime.Plan);

    [Fact] void should_report_the_registered_flat_projection() => _admission.Entries.Single(entry => entry.Kind == "projectionMirror").Status.ShouldEqual("mirrored");
}

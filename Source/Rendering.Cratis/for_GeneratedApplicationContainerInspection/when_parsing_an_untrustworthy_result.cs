// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerInspection;

public class when_parsing_an_untrustworthy_result : Specification
{
    static readonly string _id = new('a', 64);
    GeneratedApplicationContainerInspection _daemonFailure = null!;
    GeneratedApplicationContainerInspection _permissionFailure = null!;
    GeneratedApplicationContainerInspection _malformedOutput = null!;
    GeneratedApplicationContainerInspection _malformedLabels = null!;
    GeneratedApplicationContainerInspection _differentIdentifier = null!;
    GeneratedApplicationContainerInspection _outputWithMissingText = null!;

    void Because()
    {
        _daemonFailure = GeneratedApplicationContainerInspection.Parse(new(1, string.Empty, "Cannot connect to the Docker daemon"));
        _permissionFailure = GeneratedApplicationContainerInspection.Parse(new(1, string.Empty, "permission denied"));
        _malformedOutput = GeneratedApplicationContainerInspection.Parse(new(0, "garbage", string.Empty));
        _malformedLabels = GeneratedApplicationContainerInspection.Parse(new(0, $"{_id}\nnot json\n", string.Empty));
        _differentIdentifier = GeneratedApplicationContainerInspection.Parse(new(0, $"{_id}\n{{}}\n", string.Empty), new(new string('b', 64)));
        _outputWithMissingText = GeneratedApplicationContainerInspection.Parse(new(1, "partial output", "No such object"));
    }

    [Fact] void should_not_trust_a_daemon_failure() => _daemonFailure.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Unknown);
    [Fact] void should_not_trust_a_permission_failure() => _permissionFailure.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Unknown);
    [Fact] void should_not_trust_malformed_output() => _malformedOutput.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Unknown);
    [Fact] void should_not_trust_malformed_labels() => _malformedLabels.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Unknown);
    [Fact] void should_not_trust_a_different_identifier() => _differentIdentifier.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Unknown);
    [Fact] void should_not_call_a_failure_with_output_missing() => _outputWithMissingText.Kind.ShouldEqual(GeneratedApplicationContainerInspectionKind.Unknown);
}

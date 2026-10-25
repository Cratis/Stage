// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_checking_translation_constructs : Specification
{
    string? _publicEvent;
    string? _foreignCapture;
    string? _publicProjection;
    string? _foreignCommand;
    string? _unrelated;

    void Because()
    {
        _publicEvent = SemanticRunAdmission.RunsAround(null!, Feature("event", "STAGE-ESM-032: Event 'ProductPublished' is an ESM v9 public contract."));
        _publicProjection = SemanticRunAdmission.RunsAround(null!, Feature("projection", "STAGE-ESM-032: Projection 'ProductPublisher' publishes an ESM v9 public event."));
        _foreignCapture = SemanticRunAdmission.RunsAround(null!, Feature("capture", "STAGE-ESM-033: Capture 'ProductFeed' consumes ESM v9 source events."));
        _foreignCommand = SemanticRunAdmission.RunsAround(null!, Feature("command", "STAGE-ESM-033: A command reads a foreign event."));
        _unrelated = SemanticRunAdmission.RunsAround(null!, Feature("event", "STAGE-ESM-016: Not audited."));
    }

    [Fact] void should_run_a_public_event_by_appending_it_locally() => _publicEvent.ShouldContain("Published locally only");
    [Fact] void should_run_a_public_projection_by_appending_locally() => _publicProjection.ShouldContain("Published locally only");
    [Fact] void should_run_a_foreign_capture_through_seeds() => _foreignCapture.ShouldContain("foreign events arrive only through seeds");
    [Fact] void should_not_run_around_a_construct_of_another_kind() => _foreignCommand.ShouldBeNull();
    [Fact] void should_not_run_around_another_refusal() => _unrelated.ShouldBeNull();

    static SemanticAdmissionFeature Feature(string kind, string details) => new("id", kind, "ExternalEffect", details);
}

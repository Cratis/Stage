// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTriggerArtifactRenderer;

public class when_rendering_an_application_trigger : given.an_application_trigger_reaction
{
    RenderedFile _file = null!;
    SemanticApplicationTrigger _applicationTrigger = null!;
    GeneratedTypeDeclaration[] _declarations = [];

    void Establish() => _applicationTrigger = SemanticOccurrenceAdmission.Trigger(_reaction, _context);

    void Because()
    {
        _file = SemanticTriggerArtifactRenderer.Render(_located, _applicationTrigger, _context);
        _declarations = [.. SemanticCratisAdmission.GeneratedDeclarations(_context, [_located]).Where(declaration => declaration.Artifact == _applicationTrigger.Id)];
    }

    [Fact] void should_render_a_plain_payload_record() => _file.Content.ShouldContain("public record BatchRequested(string BatchId);");
    [Fact] void should_use_the_trigger_artifact_as_its_discovery_identity() => _file.Content.ShouldContain($"\"{_applicationTrigger.Id}\"");
    [Fact] void should_track_the_trigger_source() => _file.Sources.ShouldContainOnly([_applicationTrigger.Id]);
    [Fact] void should_not_be_a_logged_domain_event() => _file.Content.ShouldNotContain("EventType");
    [Fact] void should_not_be_personal_data() => _file.Content.ShouldNotContain("PII");
    [Fact] void should_use_short_type_names() => _file.Content.ShouldNotContain("global::");

    [Fact]
    void should_register_the_trigger_once_with_its_declaring_namespace() =>
        _declarations.ShouldContainOnly([new(_applicationTrigger.Id, "Trigger", "Work.Processing.Batches", "BatchRequested")]);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_moved_document_mappings : given.a_release_baseline_tree
{
    void Establish()
    {
        File.Move(Path.Combine(_root, "source.play"), Path.Combine(_root, "moved.play"));
        var state = PersistedIdentityStateSerializer.From(_workspace);
        state = state with { Documents = [.. state.Documents.Select(document => document with { Path = PortablePlayPath.Parse("moved.play") })] };
        File.WriteAllBytes(Path.Combine(_root, ".screenplay", "identities.json"), PersistedIdentityStateSerializer.Serialize(state));
    }

    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_preserve_the_document_identity() => _loaded.Success.ShouldBeTrue();
    [Fact] void should_check_the_baseline() => _loaded.Loaded!.ReleaseBaseline.Status.ShouldEqual(ReleaseBaselineAssessmentStatus.Checked);
    [Fact] void should_assess_the_rendered_revision() => _loaded.Loaded!.ReleaseBaseline.ModelRevision.ShouldEqual(_loaded.Loaded.Model.Revision);
    [Fact] void should_find_the_changed_revision() => _loaded.Loaded!.ReleaseBaseline.Violations.Single().Kind.ShouldEqual("BaselinedRevisionChanged");
}
#endif

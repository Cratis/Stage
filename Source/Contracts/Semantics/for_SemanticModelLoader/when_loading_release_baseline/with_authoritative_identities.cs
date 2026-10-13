// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Workspaces;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.when_loading_release_baseline;

public class with_authoritative_identities : given.a_release_baseline_tree
{
    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");
    [Fact] void should_still_load_the_model() => _loaded.Success.ShouldBeTrue();
    [Fact] void should_discover_the_sidecar_under_the_root() => _loaded.Loaded!.ReleaseBaseline.Violations.Single().Kind.ShouldEqual("BaselinedRevisionChanged");
    [Fact] void should_leave_the_baseline_bytes_unchanged() => File.ReadAllBytes(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName)).ShouldEqual(_baselineBytes);
    [Fact] void should_leave_the_identity_bytes_unchanged() => File.ReadAllBytes(Path.Combine(_root, ".screenplay", "identities.json")).ShouldEqual(_catalogBytes);
    [Fact] void should_create_no_other_sidecars() => Directory.GetFiles(Path.Combine(_root, ".screenplay")).Length.ShouldEqual(2);
}
#endif

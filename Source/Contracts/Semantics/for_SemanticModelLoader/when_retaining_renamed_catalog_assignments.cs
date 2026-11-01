// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_retaining_renamed_catalog_assignments : given.a_release_baseline_tree
{
    SemanticId _originalIdentity;
    SemanticAddress _renamed = null!;

    void Establish()
    {
        var catalog = _workspace.IdentityCatalog;
        var original = catalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.EventContract && assignment.Address.Name == "Recorded");
        _originalIdentity = original.Id;
        var slice = catalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice).Address;
        _renamed = SemanticAddress.ForEventContract(slice, "RecordedRenamed");
        var source = Source.Replace("Recorded", "RecordedRenamed", StringComparison.Ordinal);
        var current = Workspace(source, SemanticIdentityCatalog.Empty(catalog.Application)).IdentityCatalog;
        var renamed = SemanticIdentityCatalog.Create(
            catalog.Application,
            catalog.Documents,
            [.. current.Semantics.Select(assignment => assignment with { Id = catalog.Semantics.Single(previous => IsRenamedAddress(previous.Address, assignment.Address)).Id, Origin = SemanticIdentityOrigin.Persisted })],
            [.. current.EventContracts.Select(assignment => catalog.EventContracts.Single(previous => IsRenamedAddress(previous.Address, assignment.Address)) with { Address = assignment.Address })]);
        _workspace = Workspace(source, renamed);
        File.WriteAllText(Path.Combine(_root, "source.play"), source);
        File.WriteAllBytes(Path.Combine(_root, ".screenplay", "identities.json"), PersistedIdentityStateSerializer.Serialize(PersistedIdentityStateSerializer.From(_workspace)));
    }

    static bool IsRenamedAddress(SemanticAddress previous, SemanticAddress current) => previous.Kind == current.Kind && previous.Parts.Length == current.Parts.Length && previous.Parts.Zip(current.Parts).All(pair =>
        pair.First.Kind == pair.Second.Kind && (pair.First.Key == "Recorded" ? "RecordedRenamed" : pair.First.Key) == pair.Second.Key);

    async Task Because() => _loaded = await SemanticModelLoader.LoadAsync(_root, [], ".screenplay/identities.json", "RecordsApp");

    [Fact]
    void should_retain_every_actual_compiler_assignment()
    {
        Assert.True(_loaded.Success, string.Join("; ", _loaded.Diagnostics));
        Assert.NotEmpty(_loaded.Loaded!.SemanticIdentities);
        Assert.True(_workspace.IdentityCatalog.Semantics.SequenceEqual(_loaded.Loaded.SemanticIdentities));
    }

    [Fact]
    void should_keep_the_persisted_id_and_exact_renamed_address_together()
    {
        var assignment = _loaded.Loaded!.SemanticIdentities.Single(item => item.Id == _originalIdentity);
        Assert.Equal(_renamed, assignment.Address);
        Assert.Equal("RecordedRenamed", assignment.Address.Name);
        Assert.DoesNotContain(_loaded.Loaded.SemanticIdentities, item => item.Address.Kind == SemanticKind.EventContract && item.Address.Name == "Recorded");
        Assert.Contains(_loaded.Loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Events), @event => @event.Id == _originalIdentity && @event.Name == "RecordedRenamed");
    }
}
#endif

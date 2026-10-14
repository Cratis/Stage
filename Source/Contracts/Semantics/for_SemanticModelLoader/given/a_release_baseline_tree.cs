// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

public class a_release_baseline_tree : for_StageReleaseBaseline.given.a_release_baseline
{
    protected string _root = null!;
    protected SemanticModelLoadResult _loaded = null!;
    protected byte[] _baselineBytes = null!;
    protected byte[] _catalogBytes = null!;

    void Establish()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!File.Exists(Path.Combine(directory.FullName, "Stage.slnx"))) directory = directory.Parent!;
        _root = Path.Combine(directory.FullName, ".ai-work", "375", "loader-specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, ".screenplay"));
        _baselineBytes = ReleaseBaselineStore.Serialize(_baseline.Document!);
        _catalogBytes = SemanticIdentityCatalogSerializer.Serialize(_workspace.IdentityCatalog);
        File.WriteAllText(Path.Combine(_root, "source.play"), Source.Replace("value String", "value Bool", StringComparison.Ordinal));
        File.WriteAllBytes(Path.Combine(_root, ".screenplay", "identities.json"), _catalogBytes);
        File.WriteAllBytes(Path.Combine(_root, ".screenplay", ReleaseBaselineStore.FileName), _baselineBytes);
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets.given;

public class an_asset_directory : Specification
{
    protected string _root = null!;
    protected string _assets = null!;
    protected ModelAssets _result = null!;

    void Establish()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!File.Exists(Path.Combine(directory.FullName, "Stage.slnx"))) directory = directory.Parent;
        _root = Path.Combine(directory.FullName, ".ai-work", "stage-1041", "specs", Guid.NewGuid().ToString("N"));
        _assets = Path.Combine(_root, "assets");
        Directory.CreateDirectory(_assets);
    }

    protected void Write(string path, byte[] bytes)
    {
        var destination = Path.Combine(_assets, path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllBytes(destination, bytes);
    }

    void Destroy() => Directory.Delete(_root, recursive: true);
}
#endif

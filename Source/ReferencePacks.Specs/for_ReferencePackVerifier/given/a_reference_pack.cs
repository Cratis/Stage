// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.ReferencePacks.for_ReferencePackVerifier.given;

public class a_reference_pack : Specification
{
    // The SHA-256 of an empty file is fixed independently of the verifier.
    protected const string AssemblyHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    protected string _directory;
    protected string _packDirectory;
    protected string _assemblyPath;
    protected string _manifestPath;
    protected string[] _errors;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-reference-pack-{Guid.NewGuid():N}");
        _packDirectory = Path.Combine(_directory, "ref");
        Directory.CreateDirectory(_packDirectory);
        _assemblyPath = Path.Combine(_packDirectory, "System.Example.dll");
        File.WriteAllBytes(_assemblyPath, []);
        _manifestPath = Path.Combine(_directory, "pack.sha256");
        File.WriteAllText(_manifestPath, $"{AssemblyHash}  System.Example.dll\n");
    }

    void Destroy() => Directory.Delete(_directory, recursive: true);
}

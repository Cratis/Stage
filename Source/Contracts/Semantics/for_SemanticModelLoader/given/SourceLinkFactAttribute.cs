// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;

public sealed class SourceLinkFactAttribute : FactAttribute
{
    static readonly Lazy<string?> _skipReason = new(ProbePermission);

    public SourceLinkFactAttribute() => Skip = _skipReason.Value;

    static string? ProbePermission()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!File.Exists(Path.Combine(directory.FullName, "Stage.slnx"))) directory = directory.Parent!;
        var folder = Path.Combine(directory.FullName, ".ai-work", "210", "link-probes", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var target = Path.Combine(folder, "target");
            File.WriteAllText(target, "symlink permission probe");
            File.CreateSymbolicLink(Path.Combine(folder, "file-link"), target);
            Directory.CreateSymbolicLink(Path.Combine(folder, "directory-link"), folder);

            return null;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or PlatformNotSupportedException || (exception is IOException && (exception.HResult & 0xffff) is 1 or 13 or 1314))
        {
            return $"Symlink creation is not permitted: {exception.Message}";
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
#endif

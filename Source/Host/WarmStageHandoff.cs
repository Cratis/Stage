// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

public enum StageHandoffResult
{
    Accepted,
    Conflict,
    InvalidPath
}

public sealed record StageLoadFile(string Path, string Content);

public sealed record StageLoadRequest(Guid HandoffId, IReadOnlyList<StageLoadFile> Files);

public sealed record StageStatus(string State, StageStatusModel? Model, Guid? HandoffId);

public sealed record StageStatusModel(string Name);

public sealed class WarmStageHandoff(string modelDirectory) : IDisposable
{
    public const string HandoffFileName = ".stage-handoff-id";

    readonly SemaphoreSlim _lock = new(1, 1);
    bool _accepted;

    public static Guid? ReadHandoffId(string directory)
    {
        var path = Path.Combine(directory, HandoffFileName);
        return File.Exists(path) && Guid.TryParse(File.ReadAllText(path), out var handoffId) ? handoffId : null;
    }

    public StageStatus GetStatus() => new(_accepted ? "loading" : "warm", null, _accepted ? ReadHandoffId() : null);

    public async Task<StageHandoffResult> Load(
        StageLoadRequest request,
        Func<CancellationToken, Task> resetKernel,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_accepted)
            {
                return StageHandoffResult.Conflict;
            }

            var files = ResolveFiles(request.Files);
            if (files is null)
            {
                return StageHandoffResult.InvalidPath;
            }

            _accepted = true;
            try
            {
                RecreateModelDirectory();
                foreach (var (path, content) in files)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    await File.WriteAllTextAsync(path, content, cancellationToken);
                }
                await File.WriteAllTextAsync(Path.Combine(modelDirectory, HandoffFileName), request.HandoffId.ToString(), cancellationToken);
                await resetKernel(cancellationToken);

                return StageHandoffResult.Accepted;
            }
            catch
            {
                _accepted = false;
                throw;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    Guid? ReadHandoffId() => ReadHandoffId(modelDirectory);

    List<(string Path, string Content)>? ResolveFiles(IReadOnlyList<StageLoadFile> files)
    {
        var directory = Path.GetFullPath(modelDirectory);
        var prefix = directory.EndsWith(Path.DirectorySeparatorChar) ? directory : $"{directory}{Path.DirectorySeparatorChar}";
        var resolved = new List<(string Path, string Content)>(files.Count);

        foreach (var file in files)
        {
            var relativePath = file.Path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var path = Path.GetFullPath(Path.Combine(directory, relativePath));
            if (Path.IsPathRooted(file.Path) || !path.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }
            resolved.Add((path, file.Content));
        }

        return resolved;
    }

    void RecreateModelDirectory()
    {
        if (Directory.Exists(modelDirectory))
        {
            Directory.Delete(modelDirectory, true);
        }
        Directory.CreateDirectory(modelDirectory);
    }
}

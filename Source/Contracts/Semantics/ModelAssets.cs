// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Carries binary assets independently of the compiled semantic model and its revision.
/// </summary>
/// <param name="Files">Ordinal assets-relative paths and their immutable bytes.</param>
/// <param name="Diagnostics">Warnings for files omitted from the attachment.</param>
public sealed partial record ModelAssets(ImmutableSortedDictionary<string, ImmutableArray<byte>> Files, ImmutableArray<SemanticModelLoadDiagnostic> Diagnostics)
{
    const int MaximumFileBytes = 5 * 1024 * 1024;
    const int MaximumTotalBytes = 25 * 1024 * 1024;

    enum Omission
    {
        ByteLimit,
        BundleCollision,
        CaseCollision,
        Unreadable
    }

    /// <summary>
    /// Gets an empty attachment.
    /// </summary>
    public static ModelAssets None { get; } = new(ImmutableSortedDictionary.Create<string, ImmutableArray<byte>>(StringComparer.Ordinal), []);

    /// <summary>
    /// Reads only regular, visible files beneath the model root's assets directory. Links are never followed.
    /// </summary>
    /// <param name="modelRoot">The directory containing the model sources.</param>
    /// <returns>A deterministic binary attachment and nonblocking omission warnings.</returns>
    public static ModelAssets Read(string modelRoot)
    {
        var directory = File.Exists(modelRoot) ? Path.GetDirectoryName(modelRoot)! : modelRoot;
        var root = new DirectoryInfo(Path.Combine(Path.GetFullPath(directory), "assets"));
        return IsAssetDirectory(root) ? new AssetReader(root).Read() : None;
    }

    /// <summary>
    /// Tests whether a path is a visible, portable assets-relative file path.
    /// </summary>
    /// <param name="path">The slash-separated path.</param>
    /// <returns>Whether the path contains no hidden, traversal, rooted or unsafe components.</returns>
    public static bool IsSafePath(string path) => new RelativePath(path).IsSafe;

    internal static bool CollidesWithBundle(string path) => new RelativePath(path).CollidesWithBundle;

    static bool IsAssetDirectory(DirectoryInfo root) => root.Exists && !root.Attributes.HasFlag(FileAttributes.ReparsePoint);

    sealed class AssetReader(DirectoryInfo root)
    {
        readonly ImmutableSortedDictionary<string, ImmutableArray<byte>>.Builder _files = None.Files.ToBuilder();
        readonly ImmutableArray<SemanticModelLoadDiagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<SemanticModelLoadDiagnostic>();
        readonly HashSet<string> _names = [with(comparer: StringComparer.OrdinalIgnoreCase)];
        long _total;

        internal ModelAssets Read()
        {
            foreach (var file in Walk(root).OrderBy(file => file.FullName, StringComparer.Ordinal))
            {
                Add(new(file, RelativeTo(file)));
            }
            return new(_files.ToImmutable(), _diagnostics.ToImmutable());
        }

        static bool IsUnreadable(Exception exception) => exception is IOException or UnauthorizedAccessException;

        static bool IsVisibleEntry(FileSystemInfo entry) => !entry.Name.StartsWith('.') && !entry.Attributes.HasFlag(FileAttributes.ReparsePoint);

        IEnumerable<FileInfo> Walk(DirectoryInfo directory) => Entries(directory)
            .Where(IsVisibleEntry).OrderBy(entry => entry.FullName, StringComparer.Ordinal).SelectMany(Descendants);

        FileSystemInfo[] Entries(DirectoryInfo directory)
        {
            try
            {
                return [.. directory.EnumerateFileSystemInfos()];
            }
            catch (Exception exception) when (IsUnreadable(exception))
            {
                Warn(RelativeTo(directory), Omission.Unreadable);
                return [];
            }
        }

        RelativePath RelativeTo(FileSystemInfo entry) => new(Path.GetRelativePath(root.FullName, entry.FullName).Replace(Path.DirectorySeparatorChar, '/'));

        IEnumerable<FileInfo> Descendants(FileSystemInfo entry) => entry switch
        {
            DirectoryInfo directory => Walk(directory),
            FileInfo file => [file],
            _ => []
        };

        void Add(AssetFile asset)
        {
            if (!Include(asset.Path)) return;
            var bytes = ReadFile(asset);
            if (bytes.IsDefault) return;
            _files.Add(asset.Path.Value, bytes);
            _total += bytes.Length;
        }

        bool Include(RelativePath path)
        {
            if (!path.IsSafe) return false;
            if (path.CollidesWithBundle)
            {
                Warn(path, Omission.BundleCollision);
                return false;
            }
            if (_names.Add(path.Value)) return true;
            Warn(path, Omission.CaseCollision);
            return false;
        }

        ImmutableArray<byte> ReadFile(AssetFile asset)
        {
            try
            {
                return ReadBytes(asset);
            }
            catch (Exception exception) when (IsUnreadable(exception))
            {
                Warn(asset.Path, Omission.Unreadable);
                return default;
            }
        }

        ImmutableArray<byte> ReadBytes(AssetFile asset)
        {
            asset.File.Refresh();
            if (!Fits(asset.File.Length)) return TooLarge(asset);
            using var stream = asset.File.OpenRead();
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = stream.Read(chunk)) > 0)
            {
                buffer.Write(chunk, 0, count);
                if (!Fits(buffer.Length)) return TooLarge(asset);
            }
            return [.. buffer.ToArray()];
        }

        bool Fits(long length) => length <= MaximumFileBytes && length <= MaximumTotalBytes - _total;

        ImmutableArray<byte> TooLarge(AssetFile asset)
        {
            Warn(asset.Path, Omission.ByteLimit);
            return default;
        }

        void Warn(RelativePath path, Omission omission)
        {
            var code = omission switch
            {
                Omission.BundleCollision => "STAGE-ASSET-002",
                Omission.Unreadable => "STAGE-ASSET-003",
                _ => "STAGE-ASSET-001"
            };
            var message = omission switch
            {
                Omission.Unreadable => "The asset could not be read and was omitted.",
                Omission.BundleCollision => "The asset collides with the Vite bundle and was omitted.",
                Omission.CaseCollision => "A case-insensitive asset path collision was omitted.",
                _ => "The asset exceeds the 5 MiB per-file or 25 MiB total byte limit and was omitted."
            };
            _diagnostics.Add(new(code, $"assets/{path.Value}: {message}") { Severity = DiagnosticSeverity.Warning });
        }
    }

    sealed record AssetFile(FileInfo File, RelativePath Path);

    sealed record RelativePath(string Value)
    {
        static readonly UTF8Encoding _utf8 = new(false, true);

        internal bool IsSafe => !string.IsNullOrEmpty(Value) && HasSafeCharacters && IsNotSource && IsLossless() && HasSafeSegments;
        internal bool CollidesWithBundle => BundleNames.Pattern.IsMatch(Value);
        bool HasSafeCharacters => !Value.Any(IsUnsafeCharacter);
        bool IsNotSource => !Value.EndsWith(".play", StringComparison.OrdinalIgnoreCase);
        bool HasSafeSegments => Value.Split('/').All(segment => segment.Length > 0 && !segment.StartsWith('.'));

        static bool IsUnsafeCharacter(char character) => char.IsControl(character) || character is '\\' or ':' or '\uFFFD';

        bool IsLossless()
        {
            try
            {
                return _utf8.GetString(_utf8.GetBytes(Value)) == Value;
            }
            catch (EncoderFallbackException)
            {
                return false;
            }
        }
    }

    private static partial class BundleNames
    {
        [GeneratedRegex(@"^(index-[A-Za-z0-9_-]+\.(js|css)|[^/]+-[A-Za-z0-9_-]+(\.[^/]+)?\.map)$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
        internal static partial Regex Pattern { get; }
    }
}

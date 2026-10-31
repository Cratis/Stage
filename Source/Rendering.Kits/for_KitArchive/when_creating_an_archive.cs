// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Formats.Tar;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitArchive;

public class when_creating_an_archive : Specification
{
    ImmutableArray<byte> _archive;
    List<TarEntry> _entries = [];
    List<byte[]> _contents = [];

    void Because()
    {
        _archive = KitArchive.Write(new KitFiles(given.kit.Files()));
        using var reader = new TarReader(new MemoryStream([.. _archive]));
        while (reader.GetNextEntry() is { } entry)
        {
            _entries.Add(entry);
            using var data = new MemoryStream();
            entry.DataStream!.CopyTo(data);
            _contents.Add(data.ToArray());
        }
    }

    [Fact] void should_use_ustar() => _entries.ShouldEachConformTo(entry => entry.Format == TarEntryFormat.Ustar);
    [Fact] void should_sort_files_ordinally() => _entries.Select(entry => entry.Name).ShouldEqual(["stage-kit.json", "templates/main.sbn"]);
    [Fact] void should_fix_modification_time_to_epoch() => _entries.ShouldEachConformTo(entry => entry.ModificationTime == DateTimeOffset.UnixEpoch);
    [Fact] void should_fix_owners() => _entries.ShouldEachConformTo(entry => entry.Uid == 0 && entry.Gid == 0 && ((PosixTarEntry)entry).UserName.Length == 0 && ((PosixTarEntry)entry).GroupName.Length == 0);
    [Fact] void should_fix_mode() => _entries.ShouldEachConformTo(entry => entry.Mode == (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead));
    [Fact] void should_preserve_file_bytes() => _contents[1].ShouldEqual([.. given.kit.Bytes("hello")]);
    [Fact] void should_roundtrip_as_a_valid_kit() => KitLoader.Load(KitArchive.Read(_archive), given.kit.Capabilities).Kit!.Digest.ShouldEqual(KitDigest.Compute(_archive));
    [Fact] void should_freeze_the_canonical_digest() => KitDigest.Compute(_archive).ShouldEqual("be167d4a37e00ae65b49a5b901270eeadbe012269e1d9a0030ad452ab290b15c");
    [Fact] void should_ignore_enumeration_order() => KitArchive.Write(new KitFiles(given.kit.Files().Reverse())).AsEnumerable().ShouldEqualInOrder([.. _archive]);
    [Fact] void should_change_digest_when_manifest_changes() => given.kit.Load(given.kit.Manifest.Replace("\"id\":\"axon\"", "\"id\":\"cratis\"", StringComparison.Ordinal)).Kit!.Digest.ShouldNotEqual(KitDigest.Compute(_archive));
}

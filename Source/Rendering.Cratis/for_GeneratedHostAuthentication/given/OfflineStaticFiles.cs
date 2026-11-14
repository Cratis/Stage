// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

sealed class OfflineStaticFiles : IFileProvider
{
    public IFileInfo GetFileInfo(string subpath) => subpath == "/public.txt" ? new StaticFile() : new NotFoundFileInfo(subpath);

    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    sealed class StaticFile : IFileInfo
    {
        static readonly byte[] Content = Encoding.UTF8.GetBytes("offline-static-content");

        public bool Exists => true;
        public long Length => Content.Length;
        public string? PhysicalPath => null;
        public string Name => "public.txt";
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public bool IsDirectory => false;

        public Stream CreateReadStream() => new MemoryStream(Content, writable: false);
    }
}

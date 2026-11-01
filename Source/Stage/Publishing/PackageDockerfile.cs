// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Publishing;

/// <summary>
/// The Dockerfile that bakes a package into a Stage image.
/// </summary>
internal static class PackageDockerfile
{
    /// <summary>
    /// The folder the package lives in inside the image, and the default of <c language="shell">Stage:Published:PackagePath</c>.
    /// </summary>
    internal const string ImagePackagePath = "/stage/package";

    internal static string Render(string runtimeVersion) => $"""
        # Application image: the Stage runtime with one exported application package baked in.
        # Build from this folder:  docker build -t <registry>/<publishedName>:<applicationVersion> .
        FROM cratis/stage:{runtimeVersion}

        COPY manifest.json {ImagePackagePath}/manifest.json
        COPY model {ImagePackagePath}/model

        ENV Stage__Mode=Published
        ENV Stage__Published__PackagePath={ImagePackagePath}

        # Only the Stage HTTP port is served. Storage, authentication, trusted proxies, allowed hosts and the
        # Chronicle credentials come from the deployment's configuration and mounted secrets.
        EXPOSE 9090

        """.Replace("\r\n", "\n");
}

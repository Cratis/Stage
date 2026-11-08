// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// Serves the files of a model's assets folder, read-only, at the /assets URL path (for example a layout's logo image).
/// </summary>
internal static class StageModelAssets
{
    const string FolderName = "assets";
    const string RequestPath = "/assets";

    internal static string? FolderFor(string modelPath)
    {
        var root = Directory.Exists(modelPath) ? Path.GetFullPath(modelPath) : Path.GetDirectoryName(Path.GetFullPath(modelPath));
        var folder = root is null ? null : Path.Combine(root, FolderName);
        return folder is not null && Directory.Exists(folder) ? folder : null;
    }

    internal static void Use(IApplicationBuilder app, string modelPath)
    {
        if (FolderFor(modelPath) is not { } folder)
        {
            return;
        }

        // Rooted at the folder, so the file provider (not path joining here) refuses anything outside it, including through a symbolic link.
        // No directory browsing, default cache headers. The provider is released with the host.
        var provider = new StageContainedFileProvider(folder);
        app.ApplicationServices.GetService<IHostApplicationLifetime>()?.ApplicationStopping.Register(provider.Dispose);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = provider,
            RequestPath = RequestPath
        });
    }
}

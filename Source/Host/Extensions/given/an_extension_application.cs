// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Ui;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.Extensions.Components;
using Cratis.Stage.Host.Extensions.Packages;
using Cratis.Stage.Host.Extensions.Packages.given;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// An application whose model places a component of the Notifier extension, with the extension loaded from a lock the way a host loads it.
/// </summary>
public abstract class an_extension_application : a_package_loader
{
    protected string _modelFolder = null!;
    protected LoadedSemanticModel _loaded = null!;
    protected SceneApplication _scene = null!;
    protected ExtensionPackages _modelPackages = null!;
    protected UiContract _uiContract = null!;

    protected async Task LoadApplication()
    {
        _files = NotifierSample.Files();
        Build();
        _entry = _entry with { Reference = NotifierSample.Reference };
        _result = await Load();
        _modelFolder = Path.Combine(_folder, "model");
        Directory.CreateDirectory(_modelFolder);
        await File.WriteAllTextAsync(Path.Combine(_modelFolder, "model.play"), NotifierSample.Model);
        await File.WriteAllTextAsync(Path.Combine(_modelFolder, "screenplay.lock"), ExtensionLock.Empty.With(_entry).Format());
        ExtensionPackageHosting.UseContracts(_modelFolder, _result);

        var issues = new List<StageUnsupportedIssue>();
        var (loaded, scene) = await SemanticHost.LoadModel(_modelFolder, issues);
        if (issues.Count > 0) throw new InvalidOperationException(string.Join(" | ", issues.Select(issue => $"{issue.Capability}: {issue.Artifact}: {issue.Details}")));
        _loaded = loaded!;
        _scene = scene!;
        _uiContract = ExtensionUiContracts.Read(_modelFolder, _loaded.Model)!;
        _modelPackages = new(_result, SystemExtensionReferences.Read(_modelFolder));
    }
}

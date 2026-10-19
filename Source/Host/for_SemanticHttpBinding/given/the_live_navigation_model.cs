// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.for_StageEndpointMapper.given;
using Cratis.Stage.Semantics;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.given;

/// <summary>
/// The model the live navigation browser scenario (<c language="shell">Verification/live-navigation</c>) serves, bound on the semantic engine.
/// </summary>
public class the_live_navigation_model : a_routed_model
{
    protected IAppendSemanticFacts _facts = null!;

    async Task Establish()
    {
        var folder = Path.Combine(Root(), "Verification", "live-navigation", "model");
        var loaded = await SemanticModelLoader.LoadFromPathAsync(folder);
        _facts = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)_facts).Tail().Returns(ulong.MaxValue);
        var source = await File.ReadAllTextAsync(Path.Combine(folder, "application.play"));
        MapSemanticModel(EventModelLoader.LoadFromSource(source), loaded.Plan, _facts);
    }

    static string Root([CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Verification")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}

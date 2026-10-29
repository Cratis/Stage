// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission.given;

public static class a_model_at_an_esm_pair
{
    public static ExecutableSemanticModel From(uint major, uint minor, Func<SemanticApplication, SemanticApplication>? change = null)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered");
        var compilation = new SemanticModelCompiler().Compile("Model", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success);
        var source = compilation.Value!.Model;
        var model = ExecutableSemanticModel.Create(source.LanguageVersion, source.SemanticVersion, source.Application);
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, change?.Invoke(model.Application) ?? model.Application);
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new LanguageVersion(major, minor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, new SemanticVersion(major, minor));
        return model;
    }
}

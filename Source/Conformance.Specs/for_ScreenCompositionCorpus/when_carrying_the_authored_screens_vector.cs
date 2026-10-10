// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ScreenCompositionCorpus;

/// <summary>
/// The screens vector's authored composition, translated by Stage and carried into a runnable plan.
/// </summary>
/// <remarks>
/// The authored Scene bytes are pinned. Carrying them is still refused: the vector's guarded Close action needs
/// Scene support that the pinned packages do not have (Cratis/Scene#68, Cratis/Stage#209), and that refusal is a
/// protected guard - a runnable application must not publish an action it cannot run safely.
/// </remarks>
public class when_carrying_the_authored_screens_vector : given.the_planned_screen_composition_corpus
{
    byte[] _bytes = null!;
    Exception? _refusal;

    void Because()
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder");
        _bytes = Encoding.UTF8.GetBytes(CanonicalSceneJson.Serialize(SceneOf(form.Documents)));
        _refusal = Catch.Exception(() => Plan(form.Documents, form.IdentityCatalogBytes.AsSpan(), carryScene: true));
    }

    [Fact] void should_pin_the_authored_scene_bytes() => Convert.ToHexStringLower(SHA256.HashData(_bytes)).ShouldEqual("011149de7f54880f6d4b5ed29490f11b8aead9982d0168e1f1313c3f08e16fc7");
    [Fact] void should_pin_the_authored_scene_length() => _bytes.Length.ShouldEqual(18515);
    [Fact] void should_refuse_the_guarded_screen_action() => _refusal.ShouldBeOfExactType<UnsupportedGuardedScreenAction>();
    [Fact] void should_name_the_refusal() => _refusal!.Message.ShouldContain(UnsupportedGuardedScreenAction.DiagnosticCode);
}

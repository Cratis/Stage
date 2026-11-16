// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Realization;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ModelAssetsRendering;

public class when_roundtripping_asset_input : for_CratisArtifactRenderPlanner.given.a_register_project_render_request
{
    ModelAssets _assets = null!;
    ArtifactRenderProfile _profile = null!;
    ModelAssets _read = null!;
    bool _success;

    void Establish() => _assets = new(ModelAssets.None.Files.Add("e\u0301.svg", [0, 255]).Add("\u00E9.svg", [128, 13, 10]), []);

    void Because()
    {
        _profile = ModelAssetsInput.With(_request.Profile, _assets);
        _success = ModelAssetsInput.TryRead(_profile, out var read);
        _read = read!;
    }

    [Fact] void should_admit_the_canonical_input() => _success.ShouldBeTrue();
    [Fact] void should_preserve_ordinally_distinct_unicode_paths() => _read.Files.Keys.ShouldEqual(_assets.Files.Keys);
    [Fact] void should_preserve_bytes() => _read.Files["e\u0301.svg"].ShouldEqual(new byte[] { 0, 255 });
    [Fact] void should_replace_instead_of_duplicate_inputs() => ModelAssetsInput.With(_profile, _assets).Inputs.Count(input => input.Name == ModelAssetsInput.Name).ShouldEqual(1);
}

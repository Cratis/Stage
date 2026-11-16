// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_reading_a_single_model_file : given.an_asset_directory
{
    void Establish()
    {
        File.WriteAllText(Path.Combine(_root, "model.play"), "application Example");
        Write("logo.svg", [1, 2]);
    }

    void Because() => _result = ModelAssets.Read(Path.Combine(_root, "model.play"));
    [Fact] void should_use_the_model_directory() => _result.Files["logo.svg"].ShouldEqual(new byte[] { 1, 2 });
}
#endif

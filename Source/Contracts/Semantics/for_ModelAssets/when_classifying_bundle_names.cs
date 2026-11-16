// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_classifying_bundle_names : Specification
{
    static readonly string _hostileName = string.Concat(Enumerable.Repeat("a-", 4096)) + "!.map";

    [Theory]
    [InlineData("index-abc123.js")]
    [InlineData("index-a_b-c.css")]
    [InlineData("vendor-abc123.js.map")]
    [InlineData("index-abc123.css.map")]
    [InlineData("my-chunk-abc123.map")]
    void should_recognize_bundle_names(string name) => ModelAssets.CollidesWithBundle(name).ShouldBeTrue();

    [Theory]
    [InlineData("index-.js")]
    [InlineData("index-abc.txt")]
    [InlineData("index-a.b.js")]
    [InlineData("Index-abc123.js")]
    [InlineData("-abc123.map")]
    [InlineData("vendor-.map")]
    [InlineData("vendor-abc123..map")]
    [InlineData("vendor.map")]
    [InlineData("nested/index-abc123.js")]
    [InlineData("nested/vendor-abc123.js.map")]
    [InlineData("logo.svg")]
    void should_not_recognize_other_names(string name) => ModelAssets.CollidesWithBundle(name).ShouldBeFalse();

    [Fact] void should_classify_a_hostile_name_without_failing() => ModelAssets.CollidesWithBundle(_hostileName).ShouldBeFalse();
}
#endif

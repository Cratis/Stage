// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ProjectionConverter;

/// <summary>
/// Screenplay 5 projection references are bare event names; the kernel resolves them to the first generation.
/// A versioned reference is therefore not part of the syntax the converter has to agree with the kernel on.
/// </summary>
public class when_referencing_a_versioned_event
{
    [Fact]
    public void should_be_refused_by_the_compiler()
    {
        const string source = "module Catalog\n  feature Items\n    slice StateView Summary\n      projection Summary => SummaryModel\n        from ItemRegistered+2 key id\n          id = id";
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.False(compilation.Success);
    }
}

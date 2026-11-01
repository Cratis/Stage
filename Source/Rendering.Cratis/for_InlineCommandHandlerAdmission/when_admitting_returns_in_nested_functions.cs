// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_InlineCommandHandlerAdmission;

public class when_admitting_returns_in_nested_functions
{
    [Theory]
    [InlineData("Action finish = () => { return; }; return new OrderPlaced();")]
    [InlineData("Action finish = delegate { return; }; return new OrderPlaced();")]
    [InlineData("void Finish() { return; } return new OrderPlaced();")]
    [InlineData("Func<object> create = () => { return new OrderPlaced(); }; return;")]
    [InlineData("object Create() { return new OrderPlaced(); } return;")]
    public void should_not_treat_nested_returns_as_mixed_returns(string body) =>
        Catch.Exception(() => InlineCommandHandlerAdmission.EnsureAccepted(new("csharp", body, SourceLocation.Start), "Run", "Module.Feature.Run")).ShouldBeNull();
}

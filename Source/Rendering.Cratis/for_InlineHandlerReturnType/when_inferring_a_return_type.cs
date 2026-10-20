// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_InlineHandlerReturnType;

public class when_inferring_a_return_type
{
    [Theory]
    [InlineData("yield return new OrderPlaced();", "IEnumerable<object>")]
    [InlineData("yield break;", "IEnumerable<object>")]
    [InlineData("yield return new OrderPlaced(); return new OrderPlaced();", "IEnumerable<object>")]
    [InlineData("", "void")]
    [InlineData("Console.WriteLine(42);", "void")]
    [InlineData("if (done) return; Console.WriteLine(42);", "void")]
    [InlineData("return new OrderPlaced();", "OrderPlaced")]
    [InlineData("if (done) return new OrderPlaced(1); return new OrderPlaced(2);", "OrderPlaced")]
    [InlineData("if (done) return; return new OrderPlaced();", "OrderPlaced")]
    [InlineData("return new global::Cratis.Chronicle.EventSequences.EventForEventSourceId();", "global::Cratis.Chronicle.EventSequences.EventForEventSourceId")]
    [InlineData("if (done) return new Events . OrderPlaced(); return new Events.OrderPlaced();", "Events.OrderPlaced")]
    [InlineData("if (done) return new Batch< OrderPlaced >(); return new Batch<OrderPlaced>();", "Batch<OrderPlaced>")]
    [InlineData("return events;", "IEnumerable<object>")]
    [InlineData("return new List<object>();", "List<object>")]
    [InlineData("return new object[] { new OrderPlaced() };", "IEnumerable<object>")]
    [InlineData("if (done) return new OrderPlaced(); return new OrderCancelled();", "IEnumerable<object>")]
    [InlineData("if (done) return new OrderPlaced(); return events;", "IEnumerable<object>")]
    [InlineData("return new();", "IEnumerable<object>")]
    [InlineData("return null;", "IEnumerable<object>")]
    [InlineData("return new OrderPlaced( ;", "IEnumerable<object>")]
    [InlineData("return new OrderPlaced(); } public void Extra() {", "IEnumerable<object>")]
    [InlineData("return new OrderPlaced(); /*", "IEnumerable<object>")]
    public void should_infer_the_signature_from_the_authored_body(string body, string expected) =>
        InlineHandlerReturnType.Infer(body).ShouldEqual(expected);

    [Theory]
    [InlineData("Func<object> create = () => { return new OrderCancelled(); }; return new OrderPlaced();", "OrderPlaced")]
    [InlineData("Func<object> create = delegate { return new OrderCancelled(); }; return new OrderPlaced();", "OrderPlaced")]
    [InlineData("object Create() { return new OrderCancelled(); } return new OrderPlaced();", "OrderPlaced")]
    [InlineData("IEnumerable<object> Create() { yield return new OrderCancelled(); } return new OrderPlaced();", "OrderPlaced")]
    [InlineData("Func<object> create = () => { return new OrderPlaced(); };", "void")]
    [InlineData("Func<object> create = delegate { return new OrderPlaced(); };", "void")]
    [InlineData("object Create() { return new OrderPlaced(); }", "void")]
    [InlineData("IEnumerable<object> Create() { yield break; }", "void")]
    public void should_ignore_returns_and_yields_in_nested_functions(string body, string expected) =>
        InlineHandlerReturnType.Infer(body).ShouldEqual(expected);
}

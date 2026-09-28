// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Projections;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;
using NSubstitute;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_a_failing_projection_builder : Specification
{
    Exception _exception = null!;

    void Because()
    {
        var builder = Substitute.For<IProjectionBuilderFor<object>>();
#pragma warning disable CHR0002
        builder.When(value => value.From<string>(Arg.Any<Action<IFromBuilder<object, string>>>()))
            .Do(_ => throw new UnsupportedSemanticMapping());
#pragma warning restore CHR0002
        var method = typeof(SemanticRunProjections).GetMethod("DefineFrom", BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(object));
        _exception = Assert.Throws<UnsupportedSemanticMapping>(() => method.Invoke(
            null,
            BindingFlags.DoNotWrapExceptions,
            null,
            [builder, typeof(string), Array.Empty<(string Target, string? Source)>()],
            null));
    }

    [Fact] void should_preserve_the_error_thrown_inside_the_generic_builder() => _exception.Message.ShouldEqual("A mapping escaped semantic admission.");
}

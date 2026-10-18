// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementConverter.when_converting_to_syntax;

public class and_there_is_no_arrangement : Specification
{
    [Fact] void should_give_no_syntax() => ArrangementConverter.ToSyntax(null).ShouldBeNull();
}

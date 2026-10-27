// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class per_package_not_per_kind : given.a_declaration_plan
{
    void Establish() => _plan = new([File("class", "Order", "class-owner"), File("record", "Order", "record-owner"), File("other", "Order", "other-owner", "other")], []);
    void Because() => _declarations = new(_plan);

    [Fact] void should_rename_the_class() => _declarations.InFile("class").Single().Name.ShouldNotEqual("Order");
    [Fact] void should_rename_the_record() => _declarations.InFile("record").Single().Name.ShouldNotEqual("Order");
    [Fact] void should_keep_the_other_package_name() => _declarations.InFile("other").Single().Name.ShouldEqual("Order");
}

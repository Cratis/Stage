// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_two_imports_share_a_simple_name : given.a_file_scope
{
    JavaFileScope _reversed = null!;

    void Because()
    {
        CreateScope(new("first", "Order", []), new("second", "Order", []));
        _reversed = new(_file, new(new([_file], [])), [new("second", "Order", []), new("first", "Order", [])]);
    }

    [Fact] void should_qualify_the_first_type() => _scope.Spell(new("first", "Order", [])).ShouldEqual("first.Order");
    [Fact] void should_qualify_the_second_type() => _scope.Spell(new("second", "Order", [])).ShouldEqual("second.Order");
    [Fact] void should_have_no_first_winner() => _reversed.Spell(new("first", "Order", [])).ShouldEqual(_scope.Spell(new("first", "Order", [])));
    [Fact] void should_not_import_either_type() => _scope.Imports.Count.ShouldEqual(0);
}

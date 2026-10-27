// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class and_a_member_obscures_the_package_prefix : given.an_inheritance_scope
{
    void Establish() => _plan = _plan with { ExternalDeclarations = [new(_baseType, KnownTypes<string>.Of(["com", "Inner"])) { Supertypes = KnownTypes<TypeName>.None }] };
    void Because() => CreateInheritedScope(new TypeName("com.library", "Outer", ["Inner"]));

    [Fact] void should_avoid_both_inherited_names() => _scope.Spell(new("com.library", "Outer", ["Inner"]), "C").ShouldEqual("Outer.Inner");
    [Fact] void should_import_the_unobscured_enclosing_type() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import com.library.Outer;\n");
}

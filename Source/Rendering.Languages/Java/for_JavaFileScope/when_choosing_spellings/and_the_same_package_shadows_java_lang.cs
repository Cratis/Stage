// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_the_same_package_shadows_java_lang : given.a_file_scope
{
    void Establish() => _file = _file with { Segments = [new DeclarationSegment(new("example", "String", []), "string") { Supertypes = KnownTypes<TypeName>.None }] };
    void Because() => CreateScope(new TypeName("java.lang", "String", []));

    [Fact] void should_qualify_java_lang() => _scope.Spell(new("java.lang", "String", [])).ShouldEqual("java.lang.String");
}

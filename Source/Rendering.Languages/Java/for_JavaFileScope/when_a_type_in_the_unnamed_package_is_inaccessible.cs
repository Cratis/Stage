// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope;

public class when_a_type_in_the_unnamed_package_is_inaccessible : given.a_file_scope
{
    Exception _error = null!;

    void Establish() => CreateScope(new TypeName(string.Empty, "Widget", []));
    void Because() => _error = Catch.Exception(() => _scope.Spell(new(string.Empty, "Widget", [])));

    [Fact] void should_refuse_an_inaccessible_binding() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.ObscuredReference);
}

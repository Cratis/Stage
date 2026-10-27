// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_package_prefixes_differ_only_in_case : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish() => _plan = new([File("one", "First", "first", "com.Acme.one"), File("two", "Second", "second", "com.acme.two")], []);
    void Because() => _error = Catch.Exception(() => _declarations = new(_plan));

    [Fact] void should_refuse_the_conflicting_directory_prefix() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.CaseConflictingPackage);
}

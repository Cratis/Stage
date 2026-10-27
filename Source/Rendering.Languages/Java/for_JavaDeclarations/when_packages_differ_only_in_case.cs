// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_packages_differ_only_in_case : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish() => _plan = new([File("one", "First", "first", "com.Acme"), File("two", "Second", "second", "com.acme")], []);
    void Because() => _error = Catch.Exception(() => _declarations = new(_plan));

    [Fact] void should_refuse_case_conflicting_package_paths() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.CaseConflictingPackage);
}

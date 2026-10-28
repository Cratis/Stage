// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaImportBlock;

public class when_an_import_is_not_a_single_type : Specification
{
    Exception[] _errors = [];
    Exception _unnamed_package = null!;

    void Because()
    {
        _errors = [.. new[] { "example.*", "static example.Widget", "Widget" }
            .Select(name => Catch.Exception(() => JavaImportBlock.Render("example", [name])))];
        _unnamed_package = Catch.Exception(() => JavaImportBlock.Render(string.Empty, []));
    }

    [Fact] void should_refuse_wildcard_static_and_unqualified_imports() => _errors.ShouldEachConformTo(error => ((LanguagePackRefusal)error).Code == JavaRefusalCodes.InvalidIdentifier);
    [Fact] void should_refuse_the_default_package() => ((LanguagePackRefusal)_unnamed_package).Code.ShouldEqual(JavaRefusalCodes.DefaultPackage);
}

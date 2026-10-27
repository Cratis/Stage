// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaImportBlock;

public class when_an_import_is_not_a_single_type : Specification
{
    Exception[] _errors = [];

    void Because() => _errors = [.. new[] { new TypeName("example", "*", []), new TypeName("static example", "Widget", []), new TypeName("", "Widget", []) }
        .Select(type => Catch.Exception(() => JavaImportBlock.Render([type])))];

    [Fact] void should_refuse_wildcard_static_and_unnamed_imports() => _errors.ShouldEachConformTo(error => ((LanguagePackRefusal)error).Code == JavaRefusalCodes.InvalidIdentifier);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_a_rename_suffix_collides_with_a_declared_name : given.a_declaration_plan
{
    string _declaredName = string.Empty;

    void Establish()
    {
        _declaredName = "Foo_" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("first\none")).AsSpan(0, 4));
        _plan = new([File("one", "Foo", "first"), File("two", "Foo", "second"), File("three", _declaredName, "third")], []);
    }

    void Because() => _declarations = new(_plan);

    [Fact] void should_keep_the_genuine_declared_name() => _declarations.InFile("three").Single().Name.ShouldEqual(_declaredName);
    [Fact] void should_retry_the_generated_suffix() => _declarations.InFile("one").Single().Name.ShouldNotEqual(_declaredName);
    [Fact] void should_leave_no_collisions() => _plan.Files.SelectMany(file => _declarations.InFile(file.Id)).Select(type => type.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldEqual(3);
}

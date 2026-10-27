// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.given;

public class an_inheritance_scope : a_file_scope
{
    protected readonly TypeName _baseType = new("lib", "Base", []);
    protected SymbolPlan _plan = null!;

    void Establish()
    {
        _file = new("file", "p", [new DeclarationSegment(new("p", "C", []), "C") { Supertypes = KnownTypes<TypeName>.Of([_baseType]) }]);
        _plan = new([_file], []) { ExternalDeclarations = [new(_baseType, KnownTypes<string>.Of(["String", "Widget"])) { Supertypes = KnownTypes<TypeName>.None }] };
    }

    protected void CreateInheritedScope(params TypeName[] references) => _scope = new(
        _file,
        new(_plan with { Files = [_file, .. _plan.Files.Where(file => file.Id != _file.Id)] }),
        references);
}

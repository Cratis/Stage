// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.given;

public class a_file_scope : Specification
{
    protected SymbolFile _file = new("file", "example", [new DeclarationSegment(new("example", "Container", []), "container") { Supertypes = KnownTypes<TypeName>.None }]);
    protected JavaFileScope _scope = null!;
    protected string _spelling = string.Empty;

    protected void CreateScope(params TypeName[] references) => _scope = new(_file, new(new([_file], [])), references);
}

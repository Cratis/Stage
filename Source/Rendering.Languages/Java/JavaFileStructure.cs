// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Classifies emitted Java scopes and declaration provenance before any generated reference is shortened.
/// </summary>
/// <remarks>
/// Supports Java 21 class, interface, annotation-interface, enum and record headers (including type
/// parameters), member and local declarations, anonymous constructor bodies, enum-constant bodies,
/// ordinary blocks, lambdas and initializers. Unclassified syntax or ownership disables shortening
/// for the entire file. Opaque native scopes make no binding assertions. This is not a Java compiler:
/// dependency identities and complete supertype/member metadata remain explicit kit obligations.
/// Unknown dependency metadata is never evidence that a qualified package prefix is unobscured.
/// </remarks>
public sealed class JavaFileStructure
{
    readonly JavaRenderedFile _source;
    readonly JavaDeclarations _declarations;
    readonly Dictionary<int, int> _pairs = [];
    readonly List<JavaLexicalScope> _scopes = [];
    readonly HashSet<int> _unreliable = [];
    readonly HashSet<int> _local = [];
    readonly HashSet<int> _namespace = [];
    readonly Dictionary<int, int> _namedBodies = [];
    bool _uncertain;

    /// <summary>Initializes a new instance of the <see cref="JavaFileStructure"/> class.</summary>
    /// <param name="source">Complete provisional emitted output, with segment provenance.</param>
    /// <param name="declarations">The shared allocated declaration universe.</param>
    public JavaFileStructure(JavaRenderedFile source, JavaDeclarations declarations)
    {
        _source = source;
        _declarations = declarations;
        PairDelimiters();
        Namespace();
        NamedTypes();
        AnonymousTypes();
        OtherBraces();
        CheckContainment();
        CheckReferences();
    }

    /// <summary>Gets whether every scope and supplied owner permits file-wide shortening.</summary>
    public bool CanShorten => !_uncertain;

    /// <summary>Gets identifiers conservatively reserved outside substituted types, imports and comments.</summary>
    public IReadOnlySet<string> ReservedNames => _source.Tokens.Where((token, index) => token.Identifier && !_namespace.Contains(index) &&
        _source.Origins(token).Any(origin => _source.File.Segments[origin] is TextSegment or NativeSegment or NameSegment)).Select(token => token.Text).ToHashSet(StringComparer.Ordinal);

    /// <summary>Gets generated types that have no proven globally qualified identity (for example local types).</summary>
    public IReadOnlySet<string> UnqualifiedTypes => _scopes.Where(scope => scope.Declaration >= 0 && (_local.Contains(scope.Declaration) || _unreliable.Contains(scope.Declaration)))
        .Select(scope => _declarations.Declared(_source.File.Id, (DeclarationSegment)_source.File.Segments[scope.Declaration]).Final.QualifiedName).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Proves that actual containing scopes cannot obscure a qualified package prefix.
    /// An omitted or inconsistent caller owner never supplies evidence; actual lexical containment does.
    /// </summary>
    /// <param name="segment">The reference or placeholder segment position.</param>
    /// <param name="first">The first identifier in the qualified spelling.</param>
    /// <returns>Whether the absence of a conflicting binding is proven.</returns>
    public bool ProvesPrefix(int segment, string first)
    {
        if (_namespace.Count == 0 && _source.File.Package.Length > 0) return false;
        if (ReservedNames.Contains(first) || _declarations.PackageDeclares(_source.File.Package, first) || JavaLangTypes.Names.Contains(first)) return false;
        foreach (var scope in Containing(_source.Start(segment)))
        {
            if (scope.Kind == "unknown") return false;
            if (!scope.IsType) continue;
            if (scope.Declaration >= 0)
            {
                if (_unreliable.Contains(scope.Declaration)) return false;
                var declaration = (DeclarationSegment)_source.File.Segments[scope.Declaration];
                if (!_declarations.Inheritance.ProvesNoMemberName(_source.File.Id, declaration.Owner, first)) return false;
            }
            else if (scope.Kind is "anonymous" or "enum-constant")
            {
                if (scope.BaseType is null || !_declarations.Inheritance.ProvesNoMemberName(scope.BaseType, first)) return false;
            }
            else return false;
        }

        return true;
    }

    /// <summary>Checks that a reference occupies whole code tokens rather than a comment, literal, or merged identifier.</summary>
    /// <param name="segment">The generated reference segment identity.</param>
    /// <returns>Whether its complete emitted interval consists of its own code tokens.</returns>
    public bool IsCodeReference(int segment)
    {
        var tokens = _source.Tokens.Where(token => _source.Origins(token).Contains(segment)).ToArray();
        return tokens.Length > 0 && tokens[0].Start == _source.Start(segment) && !tokens.Any(token => token.Literal || _source.Origins(token).Count != 1);
    }

    IEnumerable<JavaLexicalScope> Containing(int position) => _scopes.Where(scope => scope.Start <= position && position < scope.End).OrderBy(scope => scope.Start);

    void PairDelimiters()
    {
        var stack = new Stack<int>();
        for (var index = 0; index < _source.Tokens.Count; index++)
        {
            var text = At(index);
            if (text is "(" or "[" or "{") stack.Push(index);
            else if (text is ")" or "]" or "}")
            {
                if (stack.Count == 0 || !Matches(At(stack.Peek()), text)) { _uncertain = true; continue; }
                var start = stack.Pop();
                _pairs[start] = index;
            }
        }

        _uncertain |= stack.Count > 0;
    }

    static bool Matches(string open, string close) => (open, close) is ("(", ")") or ("[", "]") or ("{", "}");

    string At(int index) => index >= 0 && index < _source.Tokens.Count ? _source.Tokens[index].Text : string.Empty;

    void Namespace()
    {
        var actual = string.Empty;
        for (var index = 0; index < _source.Tokens.Count; index++)
        {
            if (At(index) is not ("package" or "import")) continue;
            var start = index;
            var package = At(index) == "package";
            while (index < _source.Tokens.Count && At(index) != ";") { _namespace.Add(index); index++; }
            _namespace.Add(index);
            if (package) actual = string.Concat(_source.Tokens.Skip(start + 1).Take(index - start - 1).Select(token => token.Text));
            else _uncertain = true; // Non-structured imports are not used as shortening evidence.
        }

        if (actual != _source.File.Package) _uncertain = true;
    }

    void NamedTypes()
    {
        for (var index = 0; index + 1 < _source.Tokens.Count; index++)
        {
            if (!IsNamedType(index)) continue;
            var name = _source.Tokens[index + 1];
            var body = TypeBody(index + 2);
            if (body < 0 || !_pairs.TryGetValue(body, out var end)) { _uncertain = true; continue; }
            var origins = _source.Origins(name);
            var declaration = origins.Count == 1 && _source.File.Segments[origins[0]] is DeclarationSegment && _source.IsWholeSegment(name, origins[0]) ? origins[0] : -1;
            var native = Enumerable.Range(index, end - index + 1).All(token => _source.IsNative(_source.Tokens[token]));
            if (declaration < 0 && !native) throw new LanguagePackRefusal(JavaRefusalCodes.UnstructuredDeclaration, "An emitted generated or mixed-provenance Java type must have a DeclarationSegment as its complete name token.");
            var kind = declaration < 0 ? "opaque" : At(index);
            _scopes.Add(new(kind, _source.Tokens[index].Start, _source.Tokens[body].Start, _source.Tokens[end].End, name.Text, declaration));
            _namedBodies[body] = index;
            _uncertain |= native;
        }
    }

    bool IsNamedType(int index)
    {
        if (At(index - 1) == "." || !_source.Tokens[index + 1].Identifier) return false;
        if (At(index) is "class" or "interface" or "enum") return true;
        return At(index) == "record" && At(index + 2) is "(" or "<";
    }

    int TypeBody(int index)
    {
        for (; index < _source.Tokens.Count; index++)
        {
            if (At(index) == "{") return index;
            if (At(index) == ";") return -1;
            if (At(index) is "(" or "[")
            {
                if (!_pairs.TryGetValue(index, out var end)) return -1;
                index = end;
            }
        }

        return -1;
    }

    void AnonymousTypes()
    {
        for (var index = 0; index < _source.Tokens.Count; index++)
        {
            if (At(index) != "new") continue;
            var open = index + 1;
            while (open < _source.Tokens.Count && At(open) is not ("(" or "[" or ";" or "{")) open++;
            if (At(open) != "(" || !_pairs.TryGetValue(open, out var close) || At(close + 1) != "{" || !_pairs.TryGetValue(close + 1, out var end)) continue;
            var baseType = ErasedType(index + 1, open);
            _scopes.Add(new("anonymous", _source.Tokens[close + 1].Start, _source.Tokens[close + 1].Start, _source.Tokens[end].End, string.Empty, BaseType: baseType));
            _uncertain = true;
        }
    }

    TypeName? ErasedType(int start, int end)
    {
        var spelling = string.Concat(_source.Tokens.Skip(start).Take(end - start).TakeWhile(token => token.Text != "<").Select(token => token.Text));
        var matches = _declarations.Types.Where(type => type.Final.QualifiedName == spelling || type.Final.SimpleName == spelling && type.Package == _source.File.Package).ToArray();
        return matches.Length == 1 ? matches[0].Final : null;
    }

    void OtherBraces()
    {
        foreach (var pair in _pairs.Where(pair => At(pair.Key) == "{"))
        {
            var token = _source.Tokens[pair.Key];
            if (_scopes.Any(scope => scope.Body == token.Start)) continue;
            var parent = _scopes.Where(scope => scope.Body < token.Start && token.Start < scope.End).MaxBy(scope => scope.Body);
            var enumBody = parent is { Kind: "enum" } && !HasEnumSeparator(parent, pair.Key);
            var kind = enumBody ? "enum-constant" : BraceKind(pair.Key);
            var baseType = enumBody && parent!.Declaration >= 0 ? _declarations.Declared(_source.File.Id, (DeclarationSegment)_source.File.Segments[parent.Declaration]).Final : null;
            _scopes.Add(new(kind, token.Start, token.Start, _source.Tokens[pair.Value].End, string.Empty, BaseType: baseType));
            _uncertain |= kind is "enum-constant" or "unknown";
        }
    }

    bool HasEnumSeparator(JavaLexicalScope scope, int end) => _source.Tokens.Take(end).Any(token => token.Start > scope.Body && token.Text == ";" &&
        !_scopes.Any(child => child.Body > scope.Body && child.Body < token.Start && token.Start < child.End));

    string BraceKind(int index) => At(index - 1) switch
    {
        ")" or "else" or "try" or "finally" or "do" or "static" or "->" or ";" or "}" => "block",
        "=" or "]" or "," => "initializer",
        "{" => "block",
        _ => "unknown",
    };

    void CheckContainment()
    {
        foreach (var scope in _scopes.Where(scope => scope.Declaration >= 0))
        {
            var declaration = (DeclarationSegment)_source.File.Segments[scope.Declaration];
            var containers = Containing(scope.Start).Where(parent => parent != scope).ToArray();
            var parent = containers.LastOrDefault(candidate => candidate.IsType);
            var parentOwner = parent is { Declaration: >= 0 } ? ((DeclarationSegment)_source.File.Segments[parent.Declaration]).Owner : string.Empty;
            if (containers.Any(candidate => !candidate.IsType) || parent is { Declaration: < 0 }) _local.Add(scope.Declaration);
            if (parentOwner != declaration.ParentOwner || !HeaderMatches(scope, declaration)) _unreliable.Add(scope.Declaration);
            _uncertain |= _local.Contains(scope.Declaration) || _unreliable.Contains(scope.Declaration) || !declaration.Supertypes.IsKnown;
        }

        foreach (var (segment, index) in _source.File.Segments.Select((segment, index) => (segment, index)))
        {
            if (segment is DeclarationSegment && !_scopes.Any(scope => scope.Declaration == index))
                throw new LanguagePackRefusal(JavaRefusalCodes.UnprovenStructure, "A DeclarationSegment is not the name token of a complete emitted Java type declaration.");
        }
    }

    bool HeaderMatches(JavaLexicalScope scope, DeclarationSegment declaration)
    {
        if (!declaration.Supertypes.IsKnown) return true;
        var header = _source.Tokens.Where(token => scope.Start <= token.Start && token.Start < scope.Body).ToArray();
        var clauses = header.Select((token, index) => (token, index)).Where(item => item.token.Text is "extends" or "implements").ToArray();
        foreach (var clause in clauses)
        {
            var type = string.Concat(header.Skip(clause.index + 1).TakeWhile(token => token.Text is not ("<" or "," or "implements" or "permits")).Select(token => token.Text));
            if (!declaration.Supertypes.Values.Any(supertype => supertype.QualifiedName == type || supertype.SimpleName == type)) return false;
        }

        return true;
    }

    void CheckReferences()
    {
        foreach (var (segment, index) in _source.File.Segments.Select((segment, index) => (segment, index)))
        {
            if (segment is not ReferenceSegment reference) continue;
            var enclosing = Containing(_source.Start(index)).LastOrDefault(scope => scope.IsType);
            var actual = enclosing is { Declaration: >= 0 } ? ((DeclarationSegment)_source.File.Segments[enclosing.Declaration]).Owner : string.Empty;
            _uncertain |= reference.LexicalOwner.Length == 0 || reference.LexicalOwner != actual;
        }
    }
}

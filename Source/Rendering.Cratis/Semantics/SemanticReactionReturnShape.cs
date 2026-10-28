// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

// A deliberately bounded syntax proof, not a C# evaluator. Unknown expressions fail closed.
internal sealed class SemanticReactionReturnShape(SemanticEventContract source, SemanticApplicationContext context)
{
    static readonly HashSet<string> _collectionNames = new(StringComparer.Ordinal)
    {
        "List", "IEnumerable", "ICollection", "IList", "IReadOnlyCollection", "IReadOnlyList"
    };

    readonly Dictionary<string, Shape> _types = TypeShapes(context);
    BlockSyntax _body = null!;

    [Flags]
    enum Shape
    {
        None = 0,
        Event = 1,
        Command = 2,
        Unknown = 4
    }

    internal bool IsSeparate(string body)
    {
        _body = (BlockSyntax)SyntaxFactory.ParseStatement("{\n" + body + "\n}");
        if (_body.ContainsDiagnostics || _body.ContainsDirectives) return false;
        var shape = Combine(_body.DescendantNodes(IsHandler).Select(node => node switch
        {
            ReturnStatementSyntax statement => statement.Expression,
            YieldStatementSyntax statement => statement.Expression,
            _ => null
        }).OfType<ExpressionSyntax>());
        return !shape.HasFlag(Shape.Unknown) && shape != (Shape.Event | Shape.Command);
    }

    static bool IsHandler(SyntaxNode node) => node is not (LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

    static Dictionary<string, Shape> TypeShapes(SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var names = context.Events.Values.Select(@event => (Name: types.EventType(@event), Shape: Shape.Event))
            .Concat(context.Commands.Values.Select(command => (Name: types.SliceType(command.Id, command.Name), Shape: Shape.Command)))
            .Concat(context.Concepts.Values.Select(concept => (Name: types.CommonType(concept.Name), Shape: Shape.None)))
            .Concat(context.Types.Values.Select(type => (Name: types.CommonType(type.Name), Shape: Shape.None))).ToArray();
        var result = names.GroupBy(item => Normalize(item.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.Single().Shape : Shape.Unknown, StringComparer.Ordinal);
        foreach (var group in names.GroupBy(item => item.Name.Split('.')[^1]))
        {
            result[group.Key] = group.Count() == 1 ? group.Single().Shape : Shape.Unknown;
        }
        return result;
    }

    static string Normalize(string name) => name.Replace("global::", string.Empty, StringComparison.Ordinal);

    static bool IsList(TypeSyntax type) => type is GenericNameSyntax generic
        ? generic.Identifier.ValueText == "List"
        : Normalize(type.ToString()).StartsWith("System.Collections.Generic.List<", StringComparison.Ordinal);

    static TypeSyntax? CollectionElement(TypeSyntax type)
    {
        var name = type as GenericNameSyntax ?? (type as QualifiedNameSyntax)?.Right as GenericNameSyntax;
        if (name is not { TypeArgumentList.Arguments: [var element] }) return null;
        if (!_collectionNames.Contains(name.Identifier.ValueText)) return null;
        if (type is QualifiedNameSyntax qualified && Normalize(qualified.Left.ToString()) != "System.Collections.Generic") return null;
        return element;
    }

    // Only result-container paths are proven; a call inside a return condition can still mutate a local.
    static bool IsReturnUse(IdentifierNameSyntax use, VariableDeclaratorSyntax declaration)
    {
        var ancestors = use.Ancestors().ToArray();
        return use.SpanStart > declaration.SpanStart && ancestors.Any(IsReturn) && ancestors.All(IsHandler) &&
            ancestors.TakeWhile(node => !IsReturn(node)).All(node => IsResultExpression(node) || IsResultCollection(node));
    }

    static bool IsReturn(SyntaxNode node) => node is ReturnStatementSyntax or YieldStatementSyntax;

    static bool IsResultExpression(SyntaxNode node) => node is
        IdentifierNameSyntax or ParenthesizedExpressionSyntax or ConditionalExpressionSyntax or BinaryExpressionSyntax;

    static bool IsResultCollection(SyntaxNode node) => node is
        InitializerExpressionSyntax or ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or CollectionElementSyntax or CollectionExpressionSyntax;

    static ExpressionSyntax CollectionItem(CollectionElementSyntax element) => element switch
    {
        ExpressionElementSyntax item => item.Expression,
        SpreadElementSyntax spread => spread.Expression,
        _ => throw new InvalidOperationException("Unknown collection element syntax.")
    };

    Shape Combine(IEnumerable<ExpressionSyntax> expressions) => expressions.Aggregate(Shape.None, (shape, expression) => shape | Expression(expression));

    Shape Expression(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Expression(parenthesized.Expression),
        ConditionalExpressionSyntax conditional => Expression(conditional.WhenTrue) | Expression(conditional.WhenFalse),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression) => Expression(binary.Left) | Expression(binary.Right),
        _ => Collection(expression)
    };

    Shape Collection(ExpressionSyntax expression) => expression switch
    {
        ArrayCreationExpressionSyntax array when array.Initializer is { } initializer => Combine(initializer.Expressions),
        ImplicitArrayCreationExpressionSyntax array => Combine(array.Initializer.Expressions),
        CollectionExpressionSyntax collection => Combine(collection.Elements.Select(CollectionItem)),
        _ => Leaf(expression)
    };

    Shape Leaf(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax or TypeOfExpressionSyntax => Shape.None,
        ObjectCreationExpressionSyntax creation => Creation(creation),
        IdentifierNameSyntax identifier => Binding(identifier),
        MemberAccessExpressionSyntax member => Member(member),
        _ => Shape.Unknown
    };

    Shape Member(MemberAccessExpressionSyntax member)
    {
        if (member.Expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == "event")
        {
            return source.Properties.Any(property => Identifiers.ToPascalCase(property.Name) == member.Name.Identifier.ValueText) ? Shape.None : Shape.Unknown;
        }
        return _types.TryGetValue(Normalize(member.Expression.ToString()), out var shape) && shape == Shape.None ? Shape.None : Shape.Unknown;
    }

    Shape Creation(ObjectCreationExpressionSyntax creation)
    {
        var name = Normalize(creation.Type.ToString());
        if (_types.TryGetValue(name, out var shape)) return shape;
        if (name == "EventForEventSourceId" || name == "Cratis.Chronicle.EventSequences.EventForEventSourceId") return Shape.Event;
        if (IsList(creation.Type))
        {
            return creation.ArgumentList is { Arguments.Count: > 0 } ? Shape.Unknown : Combine(creation.Initializer?.Expressions ?? []);
        }
        return Shape.Unknown;
    }

    Shape Binding(IdentifierNameSyntax identifier)
    {
        var name = identifier.Identifier.ValueText;
        var declarations = _body.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(variable => variable.Identifier.ValueText == name).ToArray();
        if (declarations.Length == 0)
        {
            if (name == "event") return Shape.Event;
            if (name == "context" || source.Properties.Any(property => Identifiers.ToPascalCase(property.Name) == name)) return Shape.None;
            return Shape.Unknown;
        }
        return declarations is [var declaration] ? Local(declaration) : Shape.Unknown;
    }

    Shape Local(VariableDeclaratorSyntax declaration)
    {
        if (declaration.Parent is not VariableDeclarationSyntax variables) return Shape.Unknown;
        if (declaration.Initializer is null) return Shape.Unknown;
        var uses = _body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(node => node.Identifier.ValueText == declaration.Identifier.ValueText);
        if (uses.Any(use => !IsReturnUse(use, declaration))) return Shape.Unknown;
        return variables.Type.IsVar ? Expression(declaration.Initializer.Value) : Declared(variables.Type);
    }

    Shape Declared(TypeSyntax type) => type switch
    {
        ArrayTypeSyntax array => Declared(array.ElementType),
        NullableTypeSyntax nullable => Declared(nullable.ElementType),
        _ => NamedDeclared(type)
    };

    Shape NamedDeclared(TypeSyntax type)
    {
        if (_types.TryGetValue(Normalize(type.ToString()), out var shape))
        {
            return shape is Shape.Event or Shape.Command ? shape : Shape.Unknown;
        }
        return CollectionElement(type) is { } element ? Declared(element) : Shape.Unknown;
    }
}

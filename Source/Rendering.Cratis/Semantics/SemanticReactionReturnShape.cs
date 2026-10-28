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
        Unknown = 4,
        Value = 8,
        NestedCollection = 16,
        NullItem = 32
    }

    enum Position
    {
        Result,
        Item,
        Spread
    }

    internal bool HasValueItems { get; private set; }
    internal bool HasNestedCollections { get; private set; }
    internal bool HasNullItems { get; private set; }

    internal bool IsSeparate(string body)
    {
        HasValueItems = HasNestedCollections = HasNullItems = false;
        _body = (BlockSyntax)SyntaxFactory.ParseStatement("{\n" + body + "\n}");
        if (_body.ContainsDiagnostics || _body.ContainsDirectives) return false;
        var shape = _body.DescendantNodes(IsHandler).Select(node => node switch
        {
            ReturnStatementSyntax { Expression: { } expression } => Expression(expression, Position.Result),
            YieldStatementSyntax { Expression: { } expression } => Expression(expression, Position.Item),
            _ => Shape.None
        }).Aggregate(Shape.None, (shape, item) => shape | item);
        HasValueItems = shape.HasFlag(Shape.Value);
        HasNestedCollections = shape.HasFlag(Shape.NestedCollection);
        HasNullItems = shape.HasFlag(Shape.NullItem);
        return shape is Shape.None or Shape.Event or Shape.Command;
    }

    static bool IsHandler(SyntaxNode node) => node is not (LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax);

    static Dictionary<string, Shape> TypeShapes(SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var names = context.Events.Values.Select(@event => (Name: types.EventType(@event), Shape: Shape.Event))
            .Concat(context.Commands.Values.Select(command => (Name: types.SliceType(command.Id, command.Name), Shape: Shape.Command)))
            .Concat(context.Concepts.Values.Select(concept => (Name: types.CommonType(concept.Name), Shape: Shape.Value)))
            .Concat(context.Types.Values.Select(type => (Name: types.CommonType(type.Name), Shape: Shape.Value))).ToArray();
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

    static Shape Null(Position position) => position switch
    {
        Position.Item => Shape.NullItem,
        Position.Result => Shape.None,
        _ => Shape.Unknown
    };

    Shape CollectionItem(CollectionElementSyntax element) => element switch
    {
        ExpressionElementSyntax item => Expression(item.Expression, Position.Item),
        SpreadElementSyntax spread => Expression(spread.Expression, Position.Spread),
        _ => Shape.Unknown
    };

    Shape Combine(IEnumerable<ExpressionSyntax> expressions) => expressions.Aggregate(Shape.None, (shape, expression) => shape | Expression(expression, Position.Item));

    Shape Expression(ExpressionSyntax expression, Position position) => expression switch
    {
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NullLiteralExpression) => Null(position),
        ParenthesizedExpressionSyntax parenthesized => Expression(parenthesized.Expression, position),
        ConditionalExpressionSyntax conditional => Expression(conditional.WhenTrue, position) | Expression(conditional.WhenFalse, position),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.CoalesceExpression) => Expression(binary.Left, position) | Expression(binary.Right, position),
        _ => Collection(expression, position)
    };

    Shape Collection(ExpressionSyntax expression, Position position) => position == Position.Item && IsResultCollection(expression)
        ? Shape.NestedCollection
        : CollectionContents(expression, position);

    Shape CollectionContents(ExpressionSyntax expression, Position position) => expression switch
    {
        ArrayCreationExpressionSyntax array when array.Initializer is { } initializer => Combine(initializer.Expressions),
        ImplicitArrayCreationExpressionSyntax array => Combine(array.Initializer.Expressions),
        InitializerExpressionSyntax initializer => Combine(initializer.Expressions),
        CollectionExpressionSyntax collection => collection.Elements.Aggregate(Shape.None, (shape, element) => shape | CollectionItem(element)),
        _ => Leaf(expression, position)
    };

    Shape Leaf(ExpressionSyntax expression, Position position) => expression switch
    {
        ObjectCreationExpressionSyntax creation => Creation(creation, position),
        IdentifierNameSyntax identifier => Binding(identifier, position),
        _ when position == Position.Spread => Shape.Unknown,
        LiteralExpressionSyntax or TypeOfExpressionSyntax => Shape.Value,
        MemberAccessExpressionSyntax member => Member(member),
        _ => Shape.Unknown
    };

    Shape Member(MemberAccessExpressionSyntax member)
    {
        if (member.Expression is IdentifierNameSyntax identifier && IsValueBinding(identifier.Identifier.ValueText)) return Shape.Value;
        return _types.TryGetValue(Normalize(member.Expression.ToString()), out var shape) && shape == Shape.Value ? Shape.Value : Shape.Unknown;
    }

    bool IsValueBinding(string name) => name == "event" || name == "context" || source.Properties.Any(property => Identifiers.ToPascalCase(property.Name) == name);

    Shape Creation(ObjectCreationExpressionSyntax creation, Position position)
    {
        if (IsList(creation.Type)) return ListCreation(creation, position);
        return position == Position.Spread ? Shape.Unknown : NamedCreation(creation.Type);
    }

    Shape ListCreation(ObjectCreationExpressionSyntax creation, Position position)
    {
        if (position == Position.Item) return Shape.NestedCollection;
        return creation.ArgumentList is { Arguments.Count: > 0 } ? Shape.Unknown : Combine(creation.Initializer?.Expressions ?? []);
    }

    Shape NamedCreation(TypeSyntax type)
    {
        var name = Normalize(type.ToString());
        if (_types.TryGetValue(name, out var shape)) return shape;
        if (name == "EventForEventSourceId" || name == "Cratis.Chronicle.EventSequences.EventForEventSourceId") return Shape.Event;
        return Shape.Unknown;
    }

    Shape Binding(IdentifierNameSyntax identifier, Position position)
    {
        var name = identifier.Identifier.ValueText;
        var declarations = _body.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(variable => variable.Identifier.ValueText == name).ToArray();
        if (declarations.Length == 0)
        {
            if (position == Position.Spread) return Shape.Unknown;
            if (name == "event") return Shape.Event;
            if (IsValueBinding(name)) return Shape.Value;
            return Shape.Unknown;
        }
        return declarations is [var declaration] ? Local(declaration, position) : Shape.Unknown;
    }

    Shape Local(VariableDeclaratorSyntax declaration, Position position)
    {
        if (declaration.Parent is not VariableDeclarationSyntax variables) return Shape.Unknown;
        if (declaration.Initializer is null) return Shape.Unknown;
        var uses = _body.DescendantNodes().OfType<IdentifierNameSyntax>().Where(node => node.Identifier.ValueText == declaration.Identifier.ValueText);
        if (uses.Any(use => !IsReturnUse(use, declaration))) return Shape.Unknown;
        var initialized = Expression(declaration.Initializer.Value, position);
        return variables.Type.IsVar ? initialized : Declared(variables.Type, position) | (initialized & (Shape.NestedCollection | Shape.NullItem));
    }

    Shape Declared(TypeSyntax type, Position position) => type switch
    {
        ArrayTypeSyntax array => position == Position.Item || array.RankSpecifiers.Count > 1 ? Shape.NestedCollection : Declared(array.ElementType, Position.Item),
        NullableTypeSyntax nullable => Declared(nullable.ElementType, position),
        _ => NamedDeclared(type, position)
    };

    Shape NamedDeclared(TypeSyntax type, Position position)
    {
        if (CollectionElement(type) is { } element)
        {
            return position == Position.Item ? Shape.NestedCollection : Declared(element, Position.Item);
        }
        if (position == Position.Spread) return Shape.Unknown;
        return _types.TryGetValue(Normalize(type.ToString()), out var shape) ? shape : Shape.Unknown;
    }
}

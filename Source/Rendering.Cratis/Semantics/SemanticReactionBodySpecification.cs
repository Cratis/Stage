// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

// A bounded deterministic subset for generated reactor scenarios, not a sandbox or a body evaluator.
internal sealed class SemanticReactionBodySpecification(BlockSyntax body, SemanticEventContract source, Dictionary<string, SemanticId> events)
{
    readonly HashSet<string> _bindings = Bindings(body, source);

    internal static string? Rejection(SemanticApplicationContext context, SemanticSpecification specification, SemanticReactionSpecification selected)
    {
        var located = context.AllSlices.Single(slice => !slice.Slice.Reactions.IsDefault && slice.Slice.Reactions.Any(reaction => reaction.Id == selected.Reaction.Id));
        var inline = SemanticReactionBodyRendering.Inline(located, selected.Reaction, context);
        if (inline.Refused) return "The native reaction body is not admitted for rendering.";
        var body = (BlockSyntax)SyntaxFactory.ParseStatement("{\n" + inline.Code!.Text + "\n}");
        var types = new SemanticTypeSystem(context);
        var analysis = new SemanticReactionBodySpecification(body, context.Events[selected.Trigger.Source], context.Events.Values.ToDictionary(types.EventType, @event => @event.Id));
        if (!analysis.Statement(body))
            return "Native reaction specifications require deterministic event construction, input properties and local decisions; context reads, calls and other effects are not supported.";
        return EffectRejection(context, specification, analysis.Products());
    }

    static string? EffectRejection(SemanticApplicationContext context, SemanticSpecification specification, HashSet<SemanticId> products)
    {
        if (SemanticSpecificationAdmission.EventReactionTriggers(context, specification).Any(trigger => products.Contains(trigger.Source)))
            return "Generated native reactor scenarios do not settle deeper reaction cascades.";
        if (products.Any(product => SemanticSpecificationAdmission.IsReactionConstraintTarget(context, product)))
            return "Native reaction specifications do not reproduce append-time constraints.";
        return null;
    }

    static HashSet<string> Bindings(BlockSyntax body, SemanticEventContract source) =>
        [.. source.Properties.Select(property => Identifiers.ToPascalCase(property.Name)), .. body.DescendantNodes().OfType<VariableDeclaratorSyntax>().Select(variable => variable.Identifier.ValueText)];

    static bool IsReturnedInput(IdentifierNameSyntax name) => name.Identifier.Text == "@event" && name.Parent is not MemberAccessExpressionSyntax;

    HashSet<SemanticId> Products()
    {
        var constructed = body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Select(creation => creation.Type.ToString()).ToHashSet();
        var products = events.Where(pair => constructed.Contains(pair.Key) || constructed.Contains(pair.Key.Split('.')[^1])).Select(pair => pair.Value).ToHashSet();
        if (body.DescendantNodes().OfType<IdentifierNameSyntax>().Any(IsReturnedInput)) products.Add(source.Id);
        return products;
    }

    bool Statement(StatementSyntax statement) => statement switch
    {
        BlockSyntax block => block.Statements.All(Statement),
        ReturnStatementSyntax { Expression: { } expression } => Expression(expression),
        IfStatementSyntax branch => Branch(branch),
        LocalDeclarationStatementSyntax local => local.Declaration.Variables.All(InitializedLocal),
        _ => false
    };

    bool InitializedLocal(VariableDeclaratorSyntax variable) => variable.Initializer is { } initializer && Expression(initializer.Value);

    bool Branch(IfStatementSyntax branch) => Expression(branch.Condition) && Statement(branch.Statement) && (branch.Else is null || Statement(branch.Else.Statement));

    bool Expression(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax => true,
        IdentifierNameSyntax name => _bindings.Contains(name.Identifier.ValueText) || name.Identifier.Text == "@event",
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax owner } member => InputProperty(owner, member),
        ParenthesizedExpressionSyntax nested => Expression(nested.Expression),
        _ => Decision(expression) || Construction(expression)
    };

    bool InputProperty(IdentifierNameSyntax owner, MemberAccessExpressionSyntax member) => owner.Identifier.Text == "@event" && source.Properties.Any(property => Identifiers.ToPascalCase(property.Name) == member.Name.Identifier.ValueText);

    bool Decision(ExpressionSyntax expression) => expression switch
    {
        BinaryExpressionSyntax binary => Expression(binary.Left) && Expression(binary.Right),
        PrefixUnaryExpressionSyntax unary when unary.Kind() is SyntaxKind.UnaryMinusExpression or SyntaxKind.UnaryPlusExpression or SyntaxKind.LogicalNotExpression => Expression(unary.Operand),
        ConditionalExpressionSyntax conditional => Expression(conditional.Condition) && Expression(conditional.WhenTrue) && Expression(conditional.WhenFalse),
        _ => false
    };

    bool Construction(ExpressionSyntax expression) => expression switch
    {
        ObjectCreationExpressionSyntax creation => EventConstruction(creation),
        CollectionExpressionSyntax collection => collection.Elements.All(element => element is ExpressionElementSyntax item && Expression(item.Expression)),
        ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions.All(Expression),
        ImplicitArrayCreationExpressionSyntax array => array.Initializer.Expressions.All(Expression),
        _ => false
    };

    bool EventConstruction(ObjectCreationExpressionSyntax creation) => EventType(creation.Type) && creation.Initializer is null &&
        creation.ArgumentList is { } arguments && arguments.Arguments.All(argument => Expression(argument.Expression));

    bool EventType(TypeSyntax type) => events.Keys.Any(name => name == type.ToString() || name.Split('.')[^1] == type.ToString());
}

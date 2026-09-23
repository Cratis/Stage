// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.Scene;

/// <summary>
/// Composes a default screen for an application that declares none.
/// </summary>
/// <remarks>
/// <para>
/// An application whose Screenplay declares screens carries its own composition, and that is always
/// preferred. This is the fallback for everything else: without it a generated application ships a frontend
/// shell with nothing in it, and the only way to exercise the backend that was just generated is to write
/// the screen by hand.
/// </para>
/// <para>
/// It composes one command form per admitted command, bound by the command's semantic name. Native
/// String/Guid scalar descriptors use explicit inputs so required identifiers cannot be silently omitted.
/// Commands entirely covered by AutoCommandForm's default field providers use its fallback; unsupported
/// required descriptors instead receive a visible diagnostic, never a partial form.
/// </para>
/// <para>
/// After semantic admission, uniquely named optional snapshot lookups with scalar string/Guid keys get an
/// editable query input form. Only required, non-key own string/number/boolean result fields are selected,
/// in stable semantic identity order. Unsupported or ambiguous lookups are omitted, never fabricated.
/// </para>
/// </remarks>
internal static class DefaultSceneComposition
{
    /// <summary>
    /// The component that renders a command.
    /// </summary>
    const string CommandFormComponent = "Cratis.Components:commandForm";

    /// <summary>
    /// Composes the default screen for an application.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The composed Scene application, or <c language="csharp">null</c> when there is nothing to compose.</returns>
    public static SceneApplication? Create(SemanticApplicationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var commands = context.Commands.Values
            .DistinctBy(_ => _.Name, StringComparer.Ordinal)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ToArray();
        var queries = context.Queries.Values
            .GroupBy(_ => _.Name, StringComparer.Ordinal)
            .Where(_ => _.Count() == 1)
            .Select(_ => _.Single())
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .Select(query => QueryInputForm(context, query))
            .OfType<SceneElements.SceneElement>()
            .ToArray();
        if (commands.Length == 0 && queries.Length == 0)
        {
            return null;
        }

        var layout = DefaultLayout.Create();
        var elements = commands.Select(command => CommandForm(context, command)).Concat(queries).ToArray();
        var screen = new SceneScreens.Screen(
            context.Application.Name,
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                [DefaultLayout.ContentSlotName] = elements
            },
            [],
            []);

        return new SceneApplication([], [], [layout], [], [], [screen]);
    }

    /// <summary>
    /// Classifies a type for default Scene scalar descriptors.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="type">The declared type.</param>
    /// <returns>The scalar primitive, or Unknown for intentionally unsupported descriptors.</returns>
    /// <exception cref="UnsupportedSemanticRendering">The semantic type is not handled.</exception>
    internal static SemanticPrimitiveType Scalar(SemanticApplicationContext context, SemanticTypeReference type)
    {
        // Composite types and text enumerations are deliberately not scalar Scene descriptors.
        var primitive = type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            SemanticTypeReferenceKind.Concept when context.Concepts[type.Target].Values.IsEmpty => context.Concepts[type.Target].Primitive,
            SemanticTypeReferenceKind.Concept or SemanticTypeReferenceKind.CompositeType => SemanticPrimitiveType.Unknown,
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticTypeReferenceKind), type.Kind)
        };
        if (type.Kind == SemanticTypeReferenceKind.Primitive ||
            (type.Kind == SemanticTypeReferenceKind.Concept && context.Concepts[type.Target].Values.IsEmpty))
        {
            // All known scalars can reach the descriptor check; only the Scene-supported subset is selected there.
            _ = SemanticTypeSystem.Primitive(primitive);
        }

        return type.IsCollection ? SemanticPrimitiveType.Unknown : primitive;
    }

    static SceneElements.ExternalComponent? QueryInputForm(SemanticApplicationContext context, SemanticKeyedQuery query)
    {
        var keyType = Scalar(context, query.Argument.Type);
        if (query.Cardinality != SemanticQueryCardinality.ZeroOrOne || query.Delivery != SemanticQueryDelivery.Snapshot ||
            keyType is not (SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid))
        {
            return null;
        }

        var result = context.ReadModels[query.ReadModel].Properties
            .Where(property => property.Id != query.KeyProperty && !property.IsIdentifier && !property.Type.IsOptional &&
                Scalar(context, property.Type) is SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber or
                    SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean)
            .OrderBy(property => property.Id.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
        if (result is null)
        {
            return null;
        }

        var input = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["parameter"] = Identifiers.ToCamelCase(query.Argument.Name),
            ["type"] = "string",
            ["label"] = Identifiers.ToWords(query.Argument.Name),
            ["required"] = !query.Argument.Type.IsOptional
        };
        if (keyType == SemanticPrimitiveType.Uuid)
        {
            input["label"] = $"{Identifiers.ToWords(query.Argument.Name)} (GUID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)";

            // Scene validates this whole-value Unicode pattern before the native proxy can perform HTTP.
            // Require the entire canonical dashed format without trimming or normalizing the entered value.
            input["pattern"] = @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}(?![\s\S])";
        }

        return new SceneElements.ExternalComponent
        {
            Id = $"query:{query.Id}",
            Name = query.Name,
            ComponentName = "Cratis.Components:queryInputForm",
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["query"] = query.Name,
                ["inputs"] = new[] { input },
                ["resultField"] = ProxyPropertyName(result.Name),
                ["label"] = $"Find {Identifiers.ToWords(context.ReadModels[query.ReadModel].Name)}",
                ["submitLabel"] = "Search"
            }
        };
    }

    static string ProxyPropertyName(string name)
    {
        var member = Identifiers.ToPascalCase(name);

        // Arc's proxy naming preserves leading acronyms; parameters already start in Stage's camel case.
        return member.Length >= 2 && char.IsUpper(member[0]) && char.IsUpper(member[1])
            ? member
            : char.ToLowerInvariant(member[0]) + member[1..];
    }

    static SceneElements.ExternalComponent CommandForm(SemanticApplicationContext context, SemanticCommand command)
    {
        // Arc uses PascalCase CLR members for the proxy and camel-cases them except for leading acronyms.
        // A normalized collision would bind two different semantic values to one native descriptor.
        var properties = command.Properties.Select(property => (Property: property, Name: ProxyPropertyName(property.Name), Type: Scalar(context, property.Type))).ToArray();
        var collision = properties.GroupBy(_ => _.Name, StringComparer.Ordinal).FirstOrDefault(_ => _.Count() > 1);
        var explicitInputs = properties.All(_ => _.Type is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid);
        var autoInputs = properties.All(_ => _.Type is SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber or
            SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.Date);
        string? invalid = null;
        if (collision is not null)
        {
            invalid = $"Command {command.Name} cannot be composed: proxy property collision '{collision.Key}'.";
        }
        else if (!explicitInputs && !autoInputs)
        {
            invalid = $"Command {command.Name} cannot be composed: no complete field strategy for properties ({string.Join(", ", properties.Select(_ => _.Property.Name).Order(StringComparer.Ordinal))}).";
        }
        if (invalid is not null)
        {
            return new SceneElements.ExternalComponent
            {
                Id = command.Name,
                Name = command.Name,
                ComponentName = "core:text",
                Properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["text"] = invalid }
            };
        }

        var form = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = command.Name,
            ["submitLabel"] = "Submit"
        };
        if (explicitInputs && properties.Any(_ => _.Type == SemanticPrimitiveType.Uuid))
        {
            form["inputs"] = properties.OrderBy(_ => _.Name, StringComparer.Ordinal)
                .Select(_ => new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["property"] = _.Name,
                    ["type"] = _.Type == SemanticPrimitiveType.Uuid ? "guid" : "string",
                    ["label"] = string.Join(' ', Identifiers.ToWords(_.Property.Name).Split(' ').Select(word =>
                        word.Equals("id", StringComparison.OrdinalIgnoreCase) ? "ID" : char.ToUpperInvariant(word[0]) + word[1..]))
                }).ToArray();
        }

        return new SceneElements.ExternalComponent
        {
            Id = command.Name,
            Name = command.Name,
            ComponentName = CommandFormComponent,
            Properties = form
        };
    }
}

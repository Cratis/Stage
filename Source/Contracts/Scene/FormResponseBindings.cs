// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Interactions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Translates lexical response names without turning ordinary data paths into response reads.
/// </summary>
internal sealed record FormResponseBindings
{
    internal required IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Commands { get; init; }
    internal IReadOnlyDictionary<string, string> Active { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
    internal IReadOnlySet<string> Unavailable { get; init; } = new HashSet<string>(StringComparer.Ordinal);
    HashSet<string> Shadowed { get; init; } = new(StringComparer.Ordinal);

    internal static IReadOnlyDictionary<string, string> Names(CommandResponseSyntax? response) => response switch
    {
        ScalarCommandResponseSyntax scalar => new Dictionary<string, string>(StringComparer.Ordinal) { [scalar.Source.Property] = string.Empty },
        RecordCommandResponseSyntax record => record.Fields.ToDictionary(field => field.Name, field => field.Name, StringComparer.Ordinal),
        _ => new Dictionary<string, string>(StringComparer.Ordinal)
    };

    internal FormResponseBindings For(string command) => this with
    {
        Active = (Commands.GetValueOrDefault(command) ?? new Dictionary<string, string>(StringComparer.Ordinal))
            .Where(pair => !Shadowed.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
        Unavailable = new HashSet<string>(Unavailable.Concat(Active.Keys), StringComparer.Ordinal)
    };

    internal FormResponseBindings Without(IEnumerable<string> names) => this with
    {
        Shadowed = new HashSet<string>(Shadowed.Concat(names), StringComparer.Ordinal),
        Active = Active.Where(pair => !names.Contains(pair.Key, StringComparer.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
        Unavailable = new HashSet<string>(Unavailable.Except(names, StringComparer.Ordinal), StringComparer.Ordinal)
    };

    internal BindingExpression Value(BindingExpression value)
    {
        if (!IsLexicalName(value)) return value;
        if (Active.TryGetValue(value.Path, out var path)) return value with { Path = path, Kind = BindingSourceKind.CommandResponse };
        if (Unavailable.Contains(value.Path)) throw new UnsupportedFormResponseCapture(value.Path);
        return value;
    }

    internal InteractionAction Action(InteractionAction action)
    {
        var success = action is ExecuteCommandAction execute ? For(execute.Command) : this;
        var failure = action is ExecuteCommandAction ? For(string.Empty) : this;
        return Operand(action) with
        {
            Arguments = [.. action.Arguments.Select(argument => argument with { Value = Value(argument.Value) })],
            OnSuccess = [.. action.OnSuccess.Select(success.Action)],
            OnFailure = [.. action.OnFailure.Select(failure.Action)],
            OnResult = [.. action.OnResult.Select(failure.Action)]
        };
    }

    static bool IsLexicalName(BindingExpression value)
    {
        if (value.Kind is not null) return false;
        if (value.Path.Contains('.', StringComparison.Ordinal)) return false;
        if (value.Path.StartsWith('$')) return false;
        return true;
    }

    InteractionAction Operand(InteractionAction action) => action switch
    {
        SetStateAction state => state with { Value = Value(state.Value) },
        NotifyAction notify => notify with { Message = Message(notify.Message) },
        ConfirmAction confirm => confirm with { Message = Message(confirm.Message) },
        _ => action
    };

    InteractionMessage Message(InteractionMessage message) => message.Binding is { } binding ? message with { Binding = Value(binding) } : message;
}

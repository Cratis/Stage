// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSurfaceLedger;

/// <summary>
/// Guards the audited Screenplay semantic surface against drift.
/// </summary>
public class when_auditing_the_executable_model : Specification
{
    IReadOnlyDictionary<string, SemanticSurfaceDisposition> _ledger = null!;
    string[] _missing = [];
    string[] _stale = [];
    string[] _invalid = [];
    int _count;

    void Establish() => _ledger = SemanticSurfaceLedger.Entries;

    void Because()
    {
        var actual = EnumerateSurface().ToHashSet(StringComparer.Ordinal);
        var ledger = _ledger;
        _count = actual.Count;
        _missing = [.. actual.Except(ledger.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        _stale = [.. ledger.Keys.Except(actual, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
        _invalid = [.. ledger.Where(entry => entry.Value.Kind switch
            {
                SemanticSurfaceDispositionKind.Rendered => entry.Value.Detail.Length != 0,
                SemanticSurfaceDispositionKind.Rejected => !IsDiagnosticCode(entry.Value.Detail),
                SemanticSurfaceDispositionKind.Ignored => string.IsNullOrWhiteSpace(entry.Value.Detail),
                _ => true
            }).Select(entry => entry.Key).Order(StringComparer.Ordinal)];
    }

    [Fact] void should_cover_every_member_without_stale_or_invalid_entries() =>
        Assert.True(_count > 0 && _missing.Length == 0 && _stale.Length == 0 && _invalid.Length == 0,
            $"Audited {_count} semantic members. Missing ({_missing.Length}): {string.Join(", ", _missing)}; " +
            $"Stale ({_stale.Length}): {string.Join(", ", _stale)}; Invalid ({_invalid.Length}): {string.Join(", ", _invalid)}");

    static bool IsDiagnosticCode(string code) => code.StartsWith("STAGE-ESM-", StringComparison.Ordinal) &&
        code.Length == 13 && code.AsSpan(10).ToString().All(char.IsAsciiDigit);

    static IEnumerable<string> EnumerateSurface()
    {
        var assembly = typeof(ExecutableSemanticModel).Assembly;
        var types = assembly.GetExportedTypes().Where(type => type.Namespace == typeof(SemanticApplication).Namespace).ToArray();
        var pending = new Queue<Type>([typeof(ExecutableSemanticModel), typeof(SemanticApplication)]);
        var visited = new HashSet<Type>();
        while (pending.TryDequeue(out var type))
        {
            type = Unwrap(type);
            if (!types.Contains(type) || !visited.Add(type) || (!type.IsClass && !type.IsValueType))
            {
                continue;
            }

            if (type.IsEnum)
            {
                foreach (var member in Enum.GetNames(type))
                {
                    yield return $"{type.Name}.{member}";
                }

                continue;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (properties.Length == 0)
            {
                // An empty new expression/value variant must still be explicitly classified.
                yield return $"{type.Name}.$type";
            }

            foreach (var property in properties)
            {
                yield return $"{type.Name}.{property.Name}";
                pending.Enqueue(property.PropertyType);
            }

            if (type.IsAbstract)
            {
                foreach (var derived in types.Where(candidate => candidate.IsSubclassOf(type)))
                {
                    pending.Enqueue(derived);
                }
            }
        }
    }

    static Type Unwrap(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>)
        ? type.GetGenericArguments()[0]
        : type;
}

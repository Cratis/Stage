// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Execution;

namespace Cratis.Stage.Rendering.Cratis.Execution;

/// <summary>Loads and invokes verified PE units only inside the isolated worker.</summary>
public static class BodyUnitInvoker
{
    /// <summary>Evaluates a compiled unit against portable context values.</summary>
    /// <param name="cacheDirectory">The content-addressed unit cache.</param>
    /// <param name="unit">The compiler's admitted unit entry.</param>
    /// <param name="request">The evaluation context.</param>
    /// <returns>The portable verdict.</returns>
    public static BodyEvaluation Evaluate(string cacheDirectory, BodyUnitEntry unit, BodyEvaluationRequest request)
    {
        if (!Supported(unit))
            return new("refused", Diagnostic: BodyDiagnostics.Format("STAGE-IMPL-003", $"{unit.Role} is not executed by `cratis run` in Stage#891 slice S1."));
        cacheDirectory = BodyWorkerCacheDirectory.Prepare(cacheDirectory);
        if (!Verified(cacheDirectory, unit, request))
            return Fault("refused", "Body unit bytes or manifest integrity could not be verified.");
        var load = new AssemblyLoadContext($"body-{unit.UnitHash}", isCollectible: true);
        try
        {
            return Invoke(load, cacheDirectory, unit, request);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not (OutOfMemoryException or InsufficientExecutionStackException))
        {
            return new("unsatisfied", SemanticValueWire.Write(SemanticValue.Boolean(false)));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or TypeLoadException or BadImageFormatException or OverflowException or FormatException)
        {
            return Fault("faulted", "Body context could not be materialized or its entry point could not be loaded.");
        }
        finally
        {
            load.Unload();
        }
    }

    static bool Supported(BodyUnitEntry unit) =>
        Enum.TryParse<SemanticImplementationRole>(unit.Role, out var role) && BodyUnitAdmission.Supported(role);

    static bool Verified(string cacheDirectory, BodyUnitEntry unit, BodyEvaluationRequest request)
    {
        var canonical = unit.UnitHash is null ? null : BodyUnitCache.Read(cacheDirectory, unit.UnitHash);
        return unit.Admitted && unit.RequirementId == request.RequirementId && canonical is not null && Matches(canonical, unit);
    }

    static BodyEvaluation Invoke(AssemblyLoadContext load, string cacheDirectory, BodyUnitEntry unit, BodyEvaluationRequest request)
    {
        var bytes = BodyUnitCache.ReadPe(cacheDirectory, unit);
        if (bytes is null) return Fault("refused", "Body unit bytes changed before loading.");
        using var stream = new MemoryStream(bytes, writable: false);
        var assembly = load.LoadFromStream(stream);
        var wrapper = assembly.GetType(unit.WrapperType!, throwOnError: true)!;
        var context = BodyValueMaterializer.Wrapper(wrapper, request, unit);
        var method = assembly.GetType(unit.EntryType!, throwOnError: true)!.GetMethod(unit.EntryMethod!, BindingFlags.Public | BindingFlags.Static, [wrapper]);
        if (method is null) return Fault("faulted", "Body entry point is unavailable.");
        return Result(unit, method.Invoke(null, [context]));
    }

    static BodyEvaluation Result(BodyUnitEntry unit, object? result) =>
        unit.Role == nameof(SemanticImplementationRole.RulePredicate) && result is bool boolean
            ? new(boolean ? "satisfied" : "unsatisfied", SemanticValueWire.Write(SemanticValue.Boolean(boolean)))
            : Fault("refused", "Only RulePredicate bodies are executed by this slice (Stage#891 slice S1).");

    static bool Matches(BodyUnitEntry canonical, BodyUnitEntry unit) =>
        canonical.Admitted && ScalarIdentity(canonical) == ScalarIdentity(unit) &&
        MapEquals(canonical.PropertyPaths, unit.PropertyPaths) && MapEquals(canonical.PropertyNames, unit.PropertyNames);

    static (string, string, string?, string?, string?, string?, string?) ScalarIdentity(BodyUnitEntry unit) =>
        (unit.RequirementId, unit.Role, unit.PeSha256, unit.EntryType, unit.EntryMethod, unit.WrapperType, unit.BodySha256);

    static bool MapEquals(ImmutableDictionary<string, string> left, ImmutableDictionary<string, string> right) =>
        left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var value) && value == pair.Value);

    static BodyEvaluation Fault(string outcome, string message) => new(outcome, Diagnostic: BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, message));
}

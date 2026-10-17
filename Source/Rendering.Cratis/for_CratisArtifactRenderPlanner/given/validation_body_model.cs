// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Stage.Contracts.Semantics;
using FluentValidation;
using FluentValidation.Results;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class validation_body_model
{
    internal static string Command(string body, string message = " message \"Positive amount required\"", bool generated = false) => $$"""
        concept OrderId : Uuid
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId {{(generated ? "generated " : string.Empty)}}identifier
                amount Decimal
                validate
                  amount rule Positive{{message}}
                    ```csharp
                    {{body}}
                    ```
                produces OrderPlaced
                  for orderId
                  amount = amount
              event OrderPlaced
                amount Decimal
        """;

    internal static string Concept(string body) => $$"""
        concept Amount : Decimal
          validate
            rule Positive message "Positive amount required"
              ```csharp
              {{body}}
              ```
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Amount
                produces OrderPlaced
                  for orderId
                  amount = amount
              event OrderPlaced
                amount Amount
        """;

    internal static string IteratorCommand(string body) => $$"""
        concept OrderId : Uuid
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId OrderId generated identifier
                amount Decimal
                recordedAt DateTime
                validate csharp
                  ```csharp
                  {{body}}
                  ```
                produces OrderPlaced
                  for orderId
                  amount = amount
                  recordedAt = recordedAt
              event OrderPlaced
                amount Decimal
                recordedAt DateTime
        """;

    internal static string IteratorConcept(string body) => $$"""
        concept Amount : Decimal
          validate
            ```csharp
            {{body}}
            ```
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Amount
                produces OrderPlaced
                  for orderId
                  amount = amount
              event OrderPlaced
                amount Amount
        """;

    internal static LoadedSemanticModel Load(string source) => opaque_policy_model.Load(source);

    internal static ValidationResult Validate(Assembly assembly, string typeName, params object[] arguments)
    {
        var type = assembly.GetTypes().Single(type => type.Name == typeName);
        var instance = Activator.CreateInstance(type, arguments)!;
        var validator = (IValidator)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == typeName + "Validator"))!;
        var validation = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(type), instance)!;
        return validator.Validate(validation);
    }
}
#endif

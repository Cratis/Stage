// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;

internal static class routed_response_commands
{
    internal static ExecutableSemanticModel AddTo(ExecutableSemanticModel model)
    {
        var responses = invoice_model.Compile(Source);
        return ExecutableSemanticModel.Create(responses.LanguageVersion, responses.SemanticVersion, model.Application with
        {
            Concepts = [.. model.Application.Concepts, .. responses.Application.Concepts],
            EventSources = [.. model.Application.EventSources, .. responses.Application.EventSources],
            Modules = [.. model.Application.Modules, .. responses.Application.Modules]
        });
    }

    const string Source = """
        concept RoutedReceipt : Uuid
          generate uuidv7
        eventsource ResponseAccount
          identifier RoutedReceipt
          stream Transactions
            streamId String
        module Responses
          feature Recording
            slice StateChange Scalar
              command RecordScalar
                receipt RoutedReceipt generated identifier
                period String
                stream ResponseAccount.Transactions
                  streamId = period
                produces event ScalarRecorded
                  for receipt
                returns receipt
            slice StateChange Named
              command RecordNamed
                receipt RoutedReceipt generated identifier
                period String
                name String
                stream ResponseAccount.Transactions
                  streamId = period
                produces event NamedRecorded
                  for receipt
                returns
                  receipt = receipt
                  name = name
            slice StateChange Conditional
              command RecordConditional
                receipt RoutedReceipt generated identifier
                period String
                enabled Bool
                stream ResponseAccount.Transactions
                  streamId = period
                produces when enabled == true
                  ConditionalRecorded
                    for receipt
                returns receipt
              event ConditionalRecorded
        """;
}

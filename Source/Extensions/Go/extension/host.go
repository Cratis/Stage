// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package extension

import (
	"context"
	"errors"
	"fmt"
	"time"

	protocol "github.com/Cratis/Stage/Source/Extensions/Go/protocol/v1"
	"google.golang.org/grpc/metadata"
	"google.golang.org/protobuf/proto"
	"google.golang.org/protobuf/types/known/timestamppb"
)

// ErrHostUnavailable is returned by host services used before Stage has completed the handshake.
var ErrHostUnavailable = errors.New("stage has not completed the handshake with this extension, so the host services are not reachable yet")

// Host is what Stage offers an extension: settings and secrets, triggers, answering later, and logs and metrics tied
// to the invocation it belongs to.
type Host struct {
	client     protocol.StageHostClient
	credential string
	context    *protocol.RequestContext
}

func (h *Host) outgoing(ctx context.Context) (context.Context, error) {
	if h.client == nil {
		return nil, ErrHostUnavailable
	}
	return metadata.AppendToOutgoingContext(ctx, "authorization", "Bearer "+h.credential), nil
}

// Setting reads a public setting the extension declared. ok is false when it is not set.
func (h *Host) Setting(ctx context.Context, name string) (value string, ok bool, err error) {
	ctx, err = h.outgoing(ctx)
	if err != nil {
		return "", false, err
	}
	response, err := h.client.GetSetting(ctx, &protocol.GetSettingRequest{Context: h.context, Name: name})
	if err != nil {
		return "", false, err
	}
	switch response.Status {
	case protocol.GetSettingResponse_STATUS_FOUND:
		return response.Value, true, nil
	case protocol.GetSettingResponse_STATUS_NOT_SET:
		return "", false, nil
	case protocol.GetSettingResponse_STATUS_IS_SECRET:
		return "", false, fmt.Errorf("%s is declared as a secret: read it with Secret", name)
	default:
		return "", false, fmt.Errorf("%s is not a setting the extension declared", name)
	}
}

// Secret reads a secret the extension declared. Do not log it or return it from an ability. ok is false when it is not set.
func (h *Host) Secret(ctx context.Context, name string) (value string, ok bool, err error) {
	ctx, err = h.outgoing(ctx)
	if err != nil {
		return "", false, err
	}
	response, err := h.client.GetSecret(ctx, &protocol.GetSecretRequest{Context: h.context, Name: name})
	if err != nil {
		return "", false, err
	}
	switch response.Status {
	case protocol.GetSecretResponse_STATUS_FOUND:
		return response.Value, true, nil
	case protocol.GetSecretResponse_STATUS_NOT_SET:
		return "", false, nil
	case protocol.GetSecretResponse_STATUS_NOT_SECRET:
		return "", false, fmt.Errorf("%s is declared as a public setting: read it with Setting", name)
	default:
		return "", false, fmt.Errorf("%s is not a secret the extension declared", name)
	}
}

// TriggerResult is what came of raising a trigger.
type TriggerResult struct {
	// Status is Raised, UnknownTrigger, NotRouted (nothing in the model reacts to it) or Refused.
	Status string
	Detail string
}

// Raised reports whether the trigger was raised.
func (r TriggerResult) Raised() bool { return r.Status == "Raised" }

// Trigger raises a trigger the extension declared. deduplicationKey makes raising it again find the first raise;
// within an invocation an empty key defaults to the idempotency key, outside one it is required.
func (h *Host) Trigger(ctx context.Context, trigger string, payloadJSON string, deduplicationKey string) (TriggerResult, error) {
	if deduplicationKey == "" {
		deduplicationKey = h.context.IdempotencyKey
	}
	if deduplicationKey == "" {
		return TriggerResult{}, errors.New("a trigger raised outside an invocation needs a deduplication key")
	}
	ctx, err := h.outgoing(ctx)
	if err != nil {
		return TriggerResult{}, err
	}
	response, err := h.client.RaiseTrigger(ctx, &protocol.RaiseTriggerRequest{Context: h.context, Trigger: trigger, DeduplicationKey: deduplicationKey, PayloadJson: payloadJSON})
	if err != nil {
		return TriggerResult{}, err
	}
	return TriggerResult{Status: statusName(response.Status.String(), "STATUS_"), Detail: response.Detail}, nil
}

// Answer completes an invocation the extension accepted to answer later. outlet is "returned" or a declared outcome;
// an empty invocationID answers the invocation this Host belongs to. It returns the status of the answer, "Answered"
// when it was taken.
func (h *Host) Answer(ctx context.Context, outlet string, payloadJSON string, invocationID string) (string, error) {
	ctx, err := h.outgoing(ctx)
	if err != nil {
		return "", err
	}
	target := proto.Clone(h.context).(*protocol.RequestContext)
	if invocationID != "" {
		target.InvocationId = invocationID
	}
	response, err := h.client.Answer(ctx, &protocol.AnswerRequest{Context: target, Outlet: outlet, PayloadJson: payloadJSON})
	if err != nil {
		return "", err
	}
	return statusName(response.Status.String(), "STATUS_"), nil
}

// Level is the level of a log record.
type Level int

// The levels of a log record.
const (
	Trace Level = iota + 1
	Debug
	Information
	Warning
	Error
	Critical
)

// Log writes a log record tied to the invocation. Never put a secret in it.
func (h *Host) Log(ctx context.Context, level Level, message string, attributes map[string]string) error {
	ctx, err := h.outgoing(ctx)
	if err != nil {
		return err
	}
	record := &protocol.LogRecord{Level: protocol.LogLevel(level), Message: message, Occurred: timestamppb.New(time.Now()), Attributes: pairs(attributes)}
	_, err = h.client.Log(ctx, &protocol.LogRequest{Context: h.context, Records: []*protocol.LogRecord{record}})
	return err
}

// Counter adds to a counter tied to the invocation.
func (h *Host) Counter(ctx context.Context, name string, value float64, attributes map[string]string) error {
	return h.record(ctx, &protocol.Metric{Name: name, Kind: protocol.Metric_KIND_COUNTER, Value: value, Attributes: pairs(attributes)})
}

// Histogram records one measurement of a distribution, in unit such as "s".
func (h *Host) Histogram(ctx context.Context, name string, value float64, unit string, attributes map[string]string) error {
	return h.record(ctx, &protocol.Metric{Name: name, Kind: protocol.Metric_KIND_HISTOGRAM, Value: value, Unit: unit, Attributes: pairs(attributes)})
}

func (h *Host) record(ctx context.Context, metric *protocol.Metric) error {
	ctx, err := h.outgoing(ctx)
	if err != nil {
		return err
	}
	_, err = h.client.RecordMetrics(ctx, &protocol.MetricsRequest{Context: h.context, Metrics: []*protocol.Metric{metric}})
	return err
}

func pairs(attributes map[string]string) []*protocol.KeyValue {
	result := make([]*protocol.KeyValue, 0, len(attributes))
	for key, value := range attributes {
		result = append(result, &protocol.KeyValue{Key: key, Value: value})
	}
	return result
}

// statusName turns STATUS_UNKNOWN_TRIGGER into UnknownTrigger.
func statusName(name string, prefix string) string {
	name = name[len(prefix):]
	result := make([]byte, 0, len(name))
	upper := true
	for index := 0; index < len(name); index++ {
		character := name[index]
		if character == '_' {
			upper = true
			continue
		}
		if upper {
			result = append(result, character)
			upper = false
		} else if character >= 'A' && character <= 'Z' {
			result = append(result, character+'a'-'A')
		} else {
			result = append(result, character)
		}
	}
	return string(result)
}

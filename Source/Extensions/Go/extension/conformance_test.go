// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package extension_test

import (
	"context"
	"encoding/json"
	"fmt"
	"net"
	"os"
	"reflect"
	"strings"
	"sync"
	"testing"
	"time"

	"github.com/Cratis/Stage/Source/Extensions/Go/extension"
	protocol "github.com/Cratis/Stage/Source/Extensions/Go/protocol/v1"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/metadata"
	"google.golang.org/protobuf/encoding/protojson"
	"google.golang.org/protobuf/proto"
)

// conformanceExtension is the reference extension of the conformance suite, written with the Go SDK. Every SDK
// implements the same behavior; see Source/Extensions/Conformance/README.md.
func conformanceExtension() *extension.Extension {
	return extension.New("conformance-extension", "1.0.0").
		Setting("greeting", true).
		Secret("apiKey", true).
		Trigger("Notified").
		Ability("Echo", extension.Safe, func(ctx context.Context, call *extension.Call) (extension.Outcome, error) {
			_ = call.Host.Log(ctx, extension.Information, "echoing", map[string]string{"size": fmt.Sprint(len(call.Input))})
			_ = call.Host.Counter(ctx, "echo.calls", 1, nil)
			return extension.Returned(call.Input, ""), nil
		}).
		Ability("Greet", extension.Idempotent, func(ctx context.Context, call *extension.Call) (extension.Outcome, error) {
			greeting, _, err := call.Host.Setting(ctx, "greeting")
			if err != nil {
				return extension.Outcome{}, err
			}
			key, _, err := call.Host.Secret(ctx, "apiKey")
			if err != nil {
				return extension.Outcome{}, err
			}
			output, _ := json.Marshal(map[string]any{"greeting": greeting, "secretLength": len(key)})
			return extension.Returned(string(output), "greeted"), nil
		}, "greeted").
		Ability("Context", extension.Safe, func(_ context.Context, call *extension.Call) (extension.Outcome, error) {
			output, _ := json.Marshal(map[string]any{"tenant": call.Tenant, "system": call.System, "invocationId": call.InvocationID, "idempotencyKey": call.IdempotencyKey, "attempt": call.Attempt, "causation": call.Causation, "traceparent": call.Traceparent})
			return extension.Returned(string(output), ""), nil
		}).
		Ability("Notify", extension.Once, func(ctx context.Context, call *extension.Call) (extension.Outcome, error) {
			if _, err := call.Host.Trigger(ctx, "Notified", call.Input, ""); err != nil {
				return extension.Outcome{}, err
			}
			return extension.Returned("{}", ""), nil
		}).
		Ability("Defer", extension.Once, func(_ context.Context, call *extension.Call) (extension.Outcome, error) {
			// Accept now, answer later: the answer is given after the attempt has been acknowledged.
			go func() {
				time.Sleep(50 * time.Millisecond)
				_, _ = call.Host.Answer(context.Background(), "done", `{"ok":true}`, "")
			}()
			return extension.Accepted(), nil
		}, "done").
		Ability("Refuse", extension.Idempotent, func(context.Context, *extension.Call) (extension.Outcome, error) {
			return extension.NotSent("refused by design"), nil
		}).
		Ability("Reject", extension.Once, func(context.Context, *extension.Call) (extension.Outcome, error) {
			return extension.Failed("rejected by the system", true), nil
		}).
		Ability("Crash", extension.Once, func(context.Context, *extension.Call) (extension.Outcome, error) {
			return extension.Outcome{}, fmt.Errorf("boom")
		}).
		HTTP("/hook", func(_ context.Context, call *extension.HTTPCall) (extension.HTTPReply, error) {
			if call.Path != "/hook/events" {
				return extension.HTTPReply{Status: 404}, nil
			}
			return extension.HTTPReply{Status: 202, Body: append([]byte("received:"), call.Body...), Headers: map[string][]string{"X-Echo": {call.Query}}}, nil
		})
}

type hostCall struct {
	rpc           string
	request       proto.Message
	authorization string
}

// fakeHost is Stage's side of the protocol as the conformance suite fakes it.
type fakeHost struct {
	protocol.UnimplementedStageHostServer
	settings map[string]string
	secrets  map[string]string
	mutex    sync.Mutex
	calls    []hostCall
}

func (f *fakeHost) record(ctx context.Context, rpc string, request proto.Message) {
	authorization := ""
	if values := metadata.ValueFromIncomingContext(ctx, "authorization"); len(values) > 0 {
		authorization = values[0]
	}
	f.mutex.Lock()
	f.calls = append(f.calls, hostCall{rpc, request, authorization})
	f.mutex.Unlock()
}

func (f *fakeHost) snapshot() []hostCall {
	f.mutex.Lock()
	defer f.mutex.Unlock()
	return append([]hostCall(nil), f.calls...)
}

func (f *fakeHost) GetSetting(ctx context.Context, request *protocol.GetSettingRequest) (*protocol.GetSettingResponse, error) {
	f.record(ctx, "GetSetting", request)
	if value, ok := f.settings[request.Name]; ok {
		return &protocol.GetSettingResponse{Status: protocol.GetSettingResponse_STATUS_FOUND, Value: value}, nil
	}
	return &protocol.GetSettingResponse{Status: protocol.GetSettingResponse_STATUS_NOT_SET}, nil
}

func (f *fakeHost) GetSecret(ctx context.Context, request *protocol.GetSecretRequest) (*protocol.GetSecretResponse, error) {
	f.record(ctx, "GetSecret", request)
	if value, ok := f.secrets[request.Name]; ok {
		return &protocol.GetSecretResponse{Status: protocol.GetSecretResponse_STATUS_FOUND, Value: value}, nil
	}
	return &protocol.GetSecretResponse{Status: protocol.GetSecretResponse_STATUS_NOT_SET}, nil
}

func (f *fakeHost) RaiseTrigger(ctx context.Context, request *protocol.RaiseTriggerRequest) (*protocol.RaiseTriggerResponse, error) {
	f.record(ctx, "RaiseTrigger", request)
	return &protocol.RaiseTriggerResponse{Status: protocol.RaiseTriggerResponse_STATUS_RAISED}, nil
}

func (f *fakeHost) Answer(ctx context.Context, request *protocol.AnswerRequest) (*protocol.AnswerResponse, error) {
	f.record(ctx, "Answer", request)
	return &protocol.AnswerResponse{Status: protocol.AnswerResponse_STATUS_ANSWERED}, nil
}

func (f *fakeHost) Log(ctx context.Context, request *protocol.LogRequest) (*protocol.TelemetryResponse, error) {
	f.record(ctx, "Log", request)
	return &protocol.TelemetryResponse{}, nil
}

func (f *fakeHost) RecordMetrics(ctx context.Context, request *protocol.MetricsRequest) (*protocol.TelemetryResponse, error) {
	f.record(ctx, "RecordMetrics", request)
	return &protocol.TelemetryResponse{}, nil
}

func listen(t *testing.T, register func(*grpc.Server)) string {
	t.Helper()
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	server := grpc.NewServer()
	register(server)
	go func() { _ = server.Serve(listener) }()
	t.Cleanup(server.Stop)
	return listener.Addr().String()
}

// subset checks that everything expected is in actual: objects by key, arrays by index and length, scalars by value.
func subset(expected any, actual any, path string, failures *[]string, name string) {
	switch want := expected.(type) {
	case map[string]any:
		got, ok := actual.(map[string]any)
		if !ok {
			*failures = append(*failures, fmt.Sprintf("%s: %s is not an object.", name, path))
			return
		}
		for key, value := range want {
			subset(value, got[key], path+"."+key, failures, name)
		}
	case []any:
		got, ok := actual.([]any)
		if !ok || len(got) != len(want) {
			*failures = append(*failures, fmt.Sprintf("%s: %s does not have %d items.", name, path, len(want)))
			return
		}
		for index := range want {
			subset(want[index], got[index], fmt.Sprintf("%s[%d]", path, index), failures, name)
		}
	default:
		// A field that holds JSON (its name ends in Json) is compared as JSON, so key order does not matter.
		if text, ok := expected.(string); ok && strings.HasSuffix(path, "Json") {
			var wantJSON, gotJSON any
			if got, ok := actual.(string); ok && json.Unmarshal([]byte(text), &wantJSON) == nil && json.Unmarshal([]byte(got), &gotJSON) == nil {
				expected, actual = wantJSON, gotJSON
			}
		}
		if !reflect.DeepEqual(expected, actual) {
			*failures = append(*failures, fmt.Sprintf("%s: %s was %v, expected %v.", name, path, actual, expected))
		}
	}
}

func toGeneric(t *testing.T, message proto.Message) any {
	t.Helper()
	data, err := protojson.Marshal(message)
	if err != nil {
		t.Fatal(err)
	}
	var generic any
	if err := json.Unmarshal(data, &generic); err != nil {
		t.Fatal(err)
	}
	return generic
}

func TestConformance(t *testing.T) {
	data, err := os.ReadFile("../../Conformance/cases.json")
	if err != nil {
		t.Fatal(err)
	}
	var document struct {
		Token string `json:"token"`
		Host  struct {
			Settings map[string]string `json:"settings"`
			Secrets  map[string]string `json:"secrets"`
		} `json:"host"`
		Cases []struct {
			Name      string          `json:"name"`
			Rpc       string          `json:"rpc"`
			Request   json.RawMessage `json:"request"`
			Expect    any             `json:"expect"`
			HostCalls []struct {
				Rpc     string `json:"rpc"`
				Request any    `json:"request"`
			} `json:"hostCalls"`
		} `json:"cases"`
	}
	if err := json.Unmarshal(data, &document); err != nil {
		t.Fatal(err)
	}
	if len(document.Cases) < 10 {
		t.Fatalf("expected the conformance cases, found %d", len(document.Cases))
	}

	host := &fakeHost{settings: document.Host.Settings, secrets: document.Host.Secrets}
	hostAddress := listen(t, func(server *grpc.Server) { protocol.RegisterStageHostServer(server, host) })
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	sdk := conformanceExtension().NewServer()
	go func() { _ = sdk.Serve(listener) }()
	t.Cleanup(sdk.Stop)

	connection, err := grpc.NewClient(listener.Addr().String(), grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		t.Fatal(err)
	}
	defer connection.Close()
	client := protocol.NewExtensionBackendClient(connection)
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	var failures []string
	for _, c := range document.Cases {
		host.mutex.Lock()
		host.calls = nil
		host.mutex.Unlock()

		request := strings.ReplaceAll(string(c.Request), "@host", "http://"+hostAddress)
		var response proto.Message
		var callErr error
		switch c.Rpc {
		case "Describe":
			message := &protocol.DescribeRequest{}
			if err := protojson.Unmarshal([]byte(request), message); err != nil {
				t.Fatal(err)
			}
			response, callErr = client.Describe(ctx, message)
		case "Invoke":
			message := &protocol.InvokeRequest{}
			if err := protojson.Unmarshal([]byte(request), message); err != nil {
				t.Fatal(err)
			}
			response, callErr = client.Invoke(ctx, message)
		case "HandleHttp":
			message := &protocol.InboundHttpRequest{}
			if err := protojson.Unmarshal([]byte(request), message); err != nil {
				t.Fatal(err)
			}
			response, callErr = client.HandleHttp(ctx, message)
		case "Health":
			response, callErr = client.Health(ctx, &protocol.HealthRequest{})
		default:
			t.Fatalf("unknown rpc %s", c.Rpc)
		}
		if callErr != nil {
			failures = append(failures, fmt.Sprintf("%s: %v", c.Name, callErr))
			continue
		}
		subset(c.Expect, toGeneric(t, response), "response", &failures, c.Name)

		if len(c.HostCalls) > 0 {
			deadline := time.Now().Add(5 * time.Second)
			for len(host.snapshot()) < len(c.HostCalls) && time.Now().Before(deadline) {
				time.Sleep(20 * time.Millisecond)
			}
			calls := host.snapshot()
			if len(calls) != len(c.HostCalls) {
				failures = append(failures, fmt.Sprintf("%s: expected %d host calls, saw %d.", c.Name, len(c.HostCalls), len(calls)))
				continue
			}
			for index, expected := range c.HostCalls {
				if calls[index].rpc != expected.Rpc {
					failures = append(failures, fmt.Sprintf("%s: host call %d was %s, expected %s.", c.Name, index, calls[index].rpc, expected.Rpc))
				}
				if calls[index].authorization != "Bearer "+document.Token {
					failures = append(failures, fmt.Sprintf("%s: host call %d did not present the session credential.", c.Name, index))
				}
				subset(expected.Request, toGeneric(t, calls[index].request), fmt.Sprintf("host call %d", index), &failures, c.Name)
			}
		}
	}
	if len(failures) > 0 {
		t.Fatalf("the Go SDK failed %d conformance checks:\n%s", len(failures), strings.Join(failures, "\n"))
	}
}

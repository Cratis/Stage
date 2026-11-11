// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package extension

import (
	"context"
	"fmt"
	"strings"
	"sync"

	protocol "github.com/Cratis/Stage/Source/Extensions/Go/protocol/v1"
	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/status"
)

const (
	protocolMajor = 1
	protocolMinor = 0
)

type backend struct {
	protocol.UnimplementedExtensionBackendServer
	extension  *Extension
	mutex      sync.RWMutex
	connection *grpc.ClientConn
	client     protocol.StageHostClient
	credential string
}

func newBackend(extension *Extension) *backend {
	return &backend{extension: extension}
}

func (b *backend) Describe(_ context.Context, request *protocol.DescribeRequest) (*protocol.DescribeResponse, error) {
	if request.GetProtocolVersion().GetMajor() != protocolMajor {
		return nil, status.Errorf(codes.FailedPrecondition, "this SDK speaks protocol major %d; Stage speaks %d", protocolMajor, request.GetProtocolVersion().GetMajor())
	}

	// The credential arrives here and is kept in memory: nowhere else, never in the environment or a file.
	var client protocol.StageHostClient
	var connection *grpc.ClientConn
	if request.HostEndpoint != "" {
		target := strings.TrimPrefix(strings.TrimPrefix(request.HostEndpoint, "http://"), "https://")
		var err error
		connection, err = grpc.NewClient(target, grpc.WithTransportCredentials(insecure.NewCredentials()))
		if err != nil {
			return nil, status.Errorf(codes.Unavailable, "cannot reach the host services at %s: %v", request.HostEndpoint, err)
		}
		client = protocol.NewStageHostClient(connection)
	}
	b.mutex.Lock()
	if b.connection != nil {
		_ = b.connection.Close()
	}
	b.connection, b.client, b.credential = connection, client, request.SessionToken
	b.mutex.Unlock()

	response := &protocol.DescribeResponse{
		ProtocolVersion: &protocol.ProtocolVersion{Major: protocolMajor, Minor: protocolMinor},
		Name:            b.extension.name,
		Version:         b.extension.version,
	}
	for _, a := range b.extension.abilities {
		response.Abilities = append(response.Abilities, &protocol.AbilityDeclaration{Name: a.name, ContractMajor: 1, Effect: a.effect.protocol(), Outcomes: a.outcomes})
	}
	for _, trigger := range b.extension.triggers {
		response.Triggers = append(response.Triggers, &protocol.TriggerDeclaration{Name: trigger})
	}
	for _, s := range b.extension.settings {
		response.Settings = append(response.Settings, &protocol.SettingDeclaration{Name: s.name, Secret: s.secret, Required: s.required})
	}
	for _, r := range b.extension.routes {
		response.HttpRoutes = append(response.HttpRoutes, r.prefix)
	}
	return response, nil
}

func (b *backend) Invoke(ctx context.Context, request *protocol.InvokeRequest) (response *protocol.InvokeResponse, err error) {
	var found *ability
	for index := range b.extension.abilities {
		if b.extension.abilities[index].name == request.Ability {
			found = &b.extension.abilities[index]
		}
	}
	if found == nil || request.ContractMajor != 1 {
		return NotSent(fmt.Sprintf("The extension does not realize %s@%d.", request.Ability, request.ContractMajor)).response, nil
	}

	b.mutex.RLock()
	host := &Host{client: b.client, credential: b.credential, context: request.Context}
	b.mutex.RUnlock()
	requested := request.Context
	call := &Call{
		Tenant: requested.GetTenant(), System: requested.GetSystem(), InvocationID: requested.GetInvocationId(), IdempotencyKey: requested.GetIdempotencyKey(),
		Attempt: int(requested.GetAttempt()), Causation: requested.GetCausation(), Traceparent: requested.GetTraceparent(), Input: request.InputJson, Host: host,
	}
	if deadline := requested.GetDeadline(); deadline != nil {
		call.Deadline = deadline.AsTime()
	}

	// A panic or an error says nothing about whether the effect happened, so it is not ruled out.
	defer func() {
		if recovered := recover(); recovered != nil {
			response, err = Failed(fmt.Sprint(recovered), false).response, nil
		}
	}()
	outcome, failure := found.handler(ctx, call)
	if failure != nil {
		return Failed(failure.Error(), false).response, nil
	}
	if outcome.response == nil {
		return Failed("The ability answered with nothing.", false).response, nil
	}
	return outcome.response, nil
}

func (b *backend) HandleHttp(ctx context.Context, request *protocol.InboundHttpRequest) (*protocol.InboundHttpResponse, error) {
	var matched *route
	for index := range b.extension.routes {
		candidate := &b.extension.routes[index]
		if request.Path == candidate.prefix || strings.HasPrefix(request.Path, strings.TrimRight(candidate.prefix, "/")+"/") {
			if matched == nil || len(candidate.prefix) > len(matched.prefix) {
				matched = candidate
			}
		}
	}
	if matched == nil {
		return &protocol.InboundHttpResponse{Status: 404}, nil
	}

	headers := map[string][]string{}
	for _, header := range request.Headers {
		headers[header.Name] = header.Values
	}
	reply, err := matched.handler(ctx, &HTTPCall{Method: request.Method, Path: request.Path, Query: request.Query, Headers: headers, Body: request.Body})
	if err != nil {
		return &protocol.InboundHttpResponse{Status: 500}, nil
	}
	response := &protocol.InboundHttpResponse{Status: int32(reply.Status), Body: reply.Body}
	for name, values := range reply.Headers {
		response.Headers = append(response.Headers, &protocol.Header{Name: name, Values: values})
	}
	return response, nil
}

func (b *backend) Health(context.Context, *protocol.HealthRequest) (*protocol.HealthResponse, error) {
	return &protocol.HealthResponse{Status: protocol.HealthResponse_STATUS_SERVING}, nil
}

# syntax=docker/dockerfile:1
# Stage SpecRunner — a run-to-completion job. It loads a serialized event model from the mounted /model folder,
# runs its specifications and writes the results to the mounted /output folder.

ARG VERSION=0.0.0-dev
ARG COMMIT=dev

####################################
# Stage 1: Build
####################################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

ARG VERSION
ARG COMMIT

WORKDIR /src

# Copy project files and restore
COPY Directory.Build.props Directory.Packages.props global.json .editorconfig .globalconfig ./
COPY Source/Directory.Build.props ./Source/
COPY Source/Contracts/Contracts.csproj ./Source/Contracts/
COPY Source/Stage/Stage.csproj ./Source/Stage/
COPY Source/SpecRunner/SpecRunner.csproj ./Source/SpecRunner/

RUN dotnet restore ./Source/SpecRunner/SpecRunner.csproj -p:Configuration=Release

# Copy source files and publish
COPY README.md ./
COPY Source/Contracts/ ./Source/Contracts/
COPY Source/Stage/ ./Source/Stage/
COPY Source/SpecRunner/ ./Source/SpecRunner/

RUN dotnet publish ./Source/SpecRunner/SpecRunner.csproj \
    -c Release \
    -p:Version=${VERSION} \
    -p:SourceRevisionId=${COMMIT} \
    -p:EnableSourceControlManagerQueries=false \
    -p:EnableSourceLink=false \
    -o /app/out

####################################
# Stage 2: Runtime
####################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY --from=builder /app/out ./

# The event model is mounted into /model; the results land in /output.

ENTRYPOINT ["dotnet", "Cratis.Stage.SpecRunner.dll"]
CMD ["--model", "/model/event-model.json", "--output", "/output/results.json"]

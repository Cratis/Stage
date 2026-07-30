# syntax=docker/dockerfile:1
# Stage SpecRunner — a run-to-completion job. It loads a serialized event model from the mounted /model folder,
# runs its specifications and writes the results to the mounted /output folder.
#
# The SpecRunner app is a framework-dependent publish, built on the host (see .github/workflows/publish.yml or
# dockerize.sh) and only copied in here — its output is portable IL, so compiling it under QEMU emulation for
# the arm64 leg of a multi-arch image build would just waste time for no benefit.

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY Source/SpecRunner/out ./

# The event model is mounted into /model; the results land in /output.

ENTRYPOINT ["dotnet", "Cratis.Stage.SpecRunner.dll"]
CMD ["--model", "/model/event-model.json", "--output", "/output/results.json"]

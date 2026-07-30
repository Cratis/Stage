# syntax=docker/dockerfile:1
# Stage SpecRunner — a run-to-completion job. It compiles the Screenplay .play files beneath the mounted /model
# folder, runs their specifications and writes the results to the mounted /output folder.
#
# The SpecRunner app is a framework-dependent publish, built on the host (see .github/workflows/publish.yml or
# dockerize.sh) and only copied in here — its output is portable IL, so compiling it under QEMU emulation for
# the arm64 leg of a multi-arch image build would just waste time for no benefit.

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY Source/SpecRunner/out ./

# The folder of .play files is mounted into /model; the results land in /output. --model takes the folder, not a
# file — every .play file beneath it is compiled and merged into one event model.

ENTRYPOINT ["dotnet", "Cratis.Stage.SpecRunner.dll"]
CMD ["--model", "/model", "--output", "/output/results.json"]

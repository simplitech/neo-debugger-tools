FROM microsoft/dotnet:2.1.4-runtime-bionic

ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
RUN apt-get update && apt-get install -y

WORKDIR /root
RUN mkdir neo-debugger-tools
WORKDIR /root/neo-debugger/tools
COPY . .



CMD ["/bin/bash"]

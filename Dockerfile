FROM  mcr.microsoft.com/dotnet/core/sdk:2.2
COPY . .
RUN chmod +x build.sh
RUN ./build.sh


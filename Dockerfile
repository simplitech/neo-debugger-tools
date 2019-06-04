FROM  mcr.microsoft.com/dotnet/core/sdk:2.2
ENV TRAVIS_TAG local-build
RUN apt-get update -y
RUN apt-get install zip -y
COPY . .
RUN chmod +x build.sh
RUN ./build.sh ${TRAVIS_TAG}


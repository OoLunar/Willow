FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY . /src
RUN apt-get update \
    && apt install -y curl gnupg2 software-properties-common \
    && curl -sSL https://packages.microsoft.com/keys/microsoft.asc | apt-key add - \
    && apt-add-repository https://packages.microsoft.com/debian/11/prod \
    && apt-get update \
    && apt upgrade -y \
    && apt install -y libmsquic

ENTRYPOINT dotnet run -c Release --project ./Willow.Server


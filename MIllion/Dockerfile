FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src

# Copia sólo csproj para cachear el restore
COPY ["properties/properties.Api.csproj", "properties/"]
COPY ["Properties.Domain/Properties.Domain.csproj", "Properties.Domain/"]
COPY ["Properties.Infrastructure/Properties.Infrastructure.csproj", "Properties.Infrastructure/"]
RUN dotnet restore "properties/properties.Api.csproj"

# Copia el resto del código y compila
COPY . .
WORKDIR /src/properties
RUN dotnet build "properties.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish "properties.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 👇 Necesario para que VS pueda depurar dentro del contenedor
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl unzip ca-certificates procps \
 && rm -rf /var/lib/apt/lists/*

# 👇 Preinstala el depurador de VS (vsdbg) para evitar el error /root/.vs-debugger
RUN mkdir -p /vsdbg \
 && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

ENTRYPOINT ["dotnet", "properties.Api.dll"]

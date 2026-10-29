FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY UTMDesk.Api.csproj ./
RUN dotnet restore UTMDesk.Api.csproj
COPY . ./
RUN dotnet publish UTMDesk.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Render (y otros PaaS) inyectan el puerto en la variable PORT.
# Si no existe, se usa 8080 para ejecutar el contenedor localmente.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet UTMDesk.Api.dll"]

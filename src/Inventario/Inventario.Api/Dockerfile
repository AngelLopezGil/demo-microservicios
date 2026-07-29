# Etapa 1: compilar con el SDK completo
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Inventario/Inventario.Api -c Release -o /app

# Etapa 2: ejecutar con solo el runtime (imagen mucho más pequeña)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Inventario.Api.dll"]

# Etapa 1: compilar con el SDK completo
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Notificaciones/Notificaciones.Worker -c Release -o /app

# Etapa 2: ejecutar con solo el runtime (sin ASP.NET: este worker no sirve HTTP)
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Notificaciones.Worker.dll"]

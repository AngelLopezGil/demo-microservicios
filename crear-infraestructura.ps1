<#
    crear-infraestructura.ps1
    -------------------------

    Registro ejecutable de los comandos de aprovisionamiento de este entorno en Azure.

    ALCANCE Y LIMITACIONES (conscientes)
      Esto es aprovisionamiento por script, no infraestructura como codigo en
      sentido estricto: es imperativo y no idempotente, asi que ejecutarlo dos
      veces sobre el mismo grupo falla. Su valor esta en dejar registrado que
      recursos componen el entorno, con que tier y por que.
      El siguiente paso natural seria reescribirlo en Bicep, que es declarativo
      y reconcilia el estado en lugar de encadenar comandos.

      Communication Services y su dominio de correo se crean por portal: el
      asistente de dominio gestionado no tiene equivalente directo en CLI.
      No se guarda ningun secreto: la contrasena de SQL se pide al ejecutar y
      las cadenas de conexion se consultan a Azure sobre la marcha.

    Crea desde cero toda la infraestructura de Azure del proyecto demo-microservicios.

    Uso:
        .\crear-infraestructura.ps1 -Sufijo alg-7421

    Para destruirlo todo:
        az group delete --name rg-demo-microservicios --yes

    REQUISITOS PREVIOS
      - Azure CLI instalada y sesion iniciada (az login)
      - Docker Desktop arrancado (las imagenes se construyen en local: ACR Tasks
        esta bloqueado en suscripciones de credito de prueba)
      - dotnet-ef instalado (dotnet tool install --global dotnet-ef)
      - Un recurso de Azure Communication Services con dominio de correo conectado
        (ver seccion 0: no se automatiza, se crea por portal)

    QUE NO HACE ESTE SCRIPT
      - No crea el App Service de la semana 1, que quedo sustituido por Container Apps.
      - No guarda ningun secreto. La contrasena de SQL se pide al ejecutar y las
        cadenas de conexion se consultan a Azure sobre la marcha.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Sufijo,

    [string]$Grupo = "rg-demo-microservicios",
    [string]$Region = "spaincentral",

    # Recurso de Communication Services creado a mano (ver seccion 0)
    [string]$NombreAcs = "acs-demo-alg-7421",
    [Parameter(Mandatory = $true)]
    [string]$RemitenteEmail,
    [Parameter(Mandatory = $true)]
    [string]$DestinatarioEmail
)

$ErrorActionPreference = "Stop"

# Nombres derivados del sufijo
$servidorSql = "sql-demo-$Sufijo"
$namespaceSb = "sb-demo-$Sufijo"
$registro    = "acrdemo" + ($Sufijo -replace '-', '')
$entorno     = "env-demo-microservicios"

$usuarioSql = "sqladmin"
$passwordSql = Read-Host "Contrasena del administrador de SQL" -AsSecureString
$passwordSqlPlano = [System.Net.NetworkCredential]::new("", $passwordSql).Password

Write-Host "`n=== 1. Proveedores de recursos ===" -ForegroundColor Cyan
# Una suscripcion nueva no trae todos los servicios habilitados. Sin esto,
# la creacion falla con MissingSubscriptionRegistration.
$proveedores = @(
    "Microsoft.Sql",
    "Microsoft.ContainerRegistry",
    "Microsoft.App",
    "Microsoft.OperationalInsights",
    "Microsoft.ServiceBus",
    "Microsoft.KeyVault",
    "Microsoft.Insights",
    "Microsoft.Communication"
)
foreach ($p in $proveedores) {
    az provider register --namespace $p | Out-Null
}
az provider register --namespace Microsoft.Sql --wait | Out-Null

Write-Host "`n=== 2. Grupo de recursos ===" -ForegroundColor Cyan
az group create --name $Grupo --location $Region | Out-Null

Write-Host "`n=== 3. SQL Server y bases de datos ===" -ForegroundColor Cyan
# Tier Basic (~5 USD/mes cada base, facturado por hora).
# Descartado el serverless con pausa automatica: el delivery service del outbox
# y la limpieza del inbox consultan la base cada pocos segundos, asi que nunca
# se pausaria y saldria mucho mas caro.
# Descartada la oferta gratuita: no esta disponible en Spain Central.
az sql server create `
    --name $servidorSql --resource-group $Grupo --location $Region `
    --admin-user $usuarioSql --admin-password $passwordSqlPlano | Out-Null

# Regla para que los servicios de Azure alcancen el servidor.
# 0.0.0.0 es un valor especial: NO significa "internet entero", significa
# "recursos de Azure". Aun asi es amplio: incluye los de otros clientes.
az sql server firewall-rule create `
    --name permitir-servicios-azure --server $servidorSql --resource-group $Grupo `
    --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 | Out-Null

# Regla para la maquina que ejecuta este script (necesaria para las migraciones)
$miIp = (Invoke-RestMethod https://api.ipify.org)
az sql server firewall-rule create `
    --name maquina-despliegue --server $servidorSql --resource-group $Grupo `
    --start-ip-address $miIp --end-ip-address $miIp | Out-Null

foreach ($base in @("PedidosDb", "InventarioDb")) {
    az sql db create `
        --name $base --server $servidorSql --resource-group $Grupo `
        --edition Basic --backup-storage-redundancy Local | Out-Null
}

$hostSql = "$servidorSql.database.windows.net"
$cadenaPedidos    = "Server=tcp:$hostSql,1433;Initial Catalog=PedidosDb;User ID=$usuarioSql;Password=$passwordSqlPlano;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$cadenaInventario = "Server=tcp:$hostSql,1433;Initial Catalog=InventarioDb;User ID=$usuarioSql;Password=$passwordSqlPlano;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "`n=== 4. Migraciones de EF Core ===" -ForegroundColor Cyan
dotnet ef database update `
    --project src/Pedidos/Pedidos.Infrastructure `
    --startup-project src/Pedidos/Pedidos.Api `
    --connection $cadenaPedidos

dotnet ef database update `
    --project src/Inventario/Inventario.Api `
    --connection $cadenaInventario

Write-Host "`n=== 5. Service Bus ===" -ForegroundColor Cyan
# Standard es obligatorio: Basic solo ofrece colas, y el fan-out necesita
# topics con suscripciones. ~10 USD/mes de tarifa base.
# MassTransit crea la topologia (topics y suscripciones) al arrancar.
az servicebus namespace create `
    --name $namespaceSb --resource-group $Grupo --location $Region --sku Standard | Out-Null

$cadenaSb = az servicebus namespace authorization-rule keys list `
    --resource-group $Grupo --namespace-name $namespaceSb `
    --name RootManageSharedAccessKey --query primaryConnectionString -o tsv

Write-Host "`n=== 6. Container Registry ===" -ForegroundColor Cyan
# ~5 USD/mes, facturado por dia aunque no contenga ninguna imagen.
az acr create --name $registro --resource-group $Grupo --location $Region --sku Basic | Out-Null

Write-Host "`n=== 7. Construir y subir imagenes ===" -ForegroundColor Cyan
# Se construyen en local porque Microsoft bloquea ACR Tasks en suscripciones
# financiadas con credito de prueba (error TasksOperationsNotAllowed).
az acr login --name $registro

$servidorRegistro = "$registro.azurecr.io"
$imagenes = @{
    "inventario-api"        = "src/Inventario/Inventario.Api/Dockerfile"
    "pedidos-api"           = "src/Pedidos/Pedidos.Api/Dockerfile"
    "notificaciones-worker" = "src/Notificaciones/Notificaciones.Worker/Dockerfile"
}
foreach ($nombre in $imagenes.Keys) {
    docker build -t "$servidorRegistro/${nombre}:v1" -f $imagenes[$nombre] .
    docker push "$servidorRegistro/${nombre}:v1"
}

Write-Host "`n=== 8. Entorno de Container Apps ===" -ForegroundColor Cyan
# Sin destino de logs: Log Analytics factura por volumen ingerido y los logs
# de EF a nivel Information son ruido caro. Los logs se leen con
# 'az containerapp logs show'. La observabilidad real llega con Application Insights.
az extension add --name containerapp --upgrade | Out-Null
az containerapp env create `
    --name $entorno --resource-group $Grupo --location $Region `
    --logs-destination none | Out-Null

Write-Host "`n=== 9. Cadena de conexion de Communication Services ===" -ForegroundColor Cyan
$cadenaEmail = az communication list-key `
    --name $NombreAcs --resource-group $Grupo --query primaryConnectionString -o tsv

Write-Host "`n=== 10. Container Apps ===" -ForegroundColor Cyan
# min-replicas 1: son consumidores de mensajes. A cero no consumirian nada,
#   y sin una regla KEDA nada podria despertarlos.
# max-replicas 1: bloquea el escalado automatico a proposito. Al escalar por
#   encima del minimo, TODAS las replicas pasan a tarifa activa (8x mas cara).
# Medido en produccion: con esta configuracion las tres replicas se facturan
#   siempre en reposo, ~16 USD/mes entre las tres.
# --registry-identity system: identidad administrada para leer del registro,
#   sin usuario ni contrasena.

az containerapp create `
    --name ca-inventario --resource-group $Grupo --environment $entorno `
    --image "$servidorRegistro/inventario-api:v1" `
    --registry-server $servidorRegistro --registry-identity system `
    --target-port 8080 --ingress external `
    --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 `
    --secrets "sqlconn=$cadenaInventario" "sbconn=$cadenaSb" `
    --env-vars "ASPNETCORE_ENVIRONMENT=Development" `
               "ConnectionStrings__InventarioDb=secretref:sqlconn" `
               "ConnectionStrings__ServiceBus=secretref:sbconn" | Out-Null

az containerapp create `
    --name ca-pedidos --resource-group $Grupo --environment $entorno `
    --image "$servidorRegistro/pedidos-api:v1" `
    --registry-server $servidorRegistro --registry-identity system `
    --target-port 8080 --ingress external `
    --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 `
    --secrets "sqlconn=$cadenaPedidos" "sbconn=$cadenaSb" `
    --env-vars "ASPNETCORE_ENVIRONMENT=Development" `
               "ConnectionStrings__PedidosDb=secretref:sqlconn" `
               "ConnectionStrings__ServiceBus=secretref:sbconn" | Out-Null

# Sin ingress ni target-port: es un worker, no escucha HTTP.
# Su Dockerfile usa la imagen base 'runtime' en vez de 'aspnet' por lo mismo.
az containerapp create `
    --name ca-notificaciones --resource-group $Grupo --environment $entorno `
    --image "$servidorRegistro/notificaciones-worker:v1" `
    --registry-server $servidorRegistro --registry-identity system `
    --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 `
    --secrets "sbconn=$cadenaSb" "emailconn=$cadenaEmail" `
    --env-vars "ConnectionStrings__ServiceBus=secretref:sbconn" `
               "ConnectionStrings__Email=secretref:emailconn" `
               "Email__Remitente=$RemitenteEmail" `
               "Email__Destinatario=$DestinatarioEmail" | Out-Null

Write-Host "`n=== Listo ===" -ForegroundColor Green
az containerapp list --resource-group $Grupo `
    --query "[].{app:name, url:properties.configuration.ingress.fqdn}" -o table

Write-Host "`nPara destruirlo todo: az group delete --name $Grupo --yes" -ForegroundColor Yellow

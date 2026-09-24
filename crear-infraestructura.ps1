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

      No se guarda ni se pide ningun secreto: el sistema no tiene ninguno.

    PASOS QUE NO AUTOMATIZA (requieren intervencion manual, ver secciones 0 y 9)
      - Communication Services y su dominio de correo: el asistente de dominio
        gestionado no tiene equivalente directo en CLI.
      - Los usuarios de base de datos para las identidades administradas: es
        T-SQL, no comandos az, y debe ejecutarse despues de crear las apps.

    LIMITACION NO VERIFICADA
      Los roles de Service Bus que asigna (Data Sender / Data Receiver) han sido
      probados sobre una topologia que YA EXISTIA. No esta verificado que basten
      para que MassTransit cree topics y suscripciones en un namespace vacio.
      Si el arranque falla con un error de autorizacion en un despliegue desde
      cero, la solucion es asignar temporalmente "Azure Service Bus Data Owner",
      dejar que se cree la topologia, y volver a los roles acotados.

    Uso:
        .\crear-infraestructura.ps1 -Sufijo alg-7421 -RemitenteEmail "DoNotReply@xxx.azurecomm.net" -DestinatarioEmail "tu@correo.com"

    Para destruirlo todo:
        az group delete --name rg-demo-microservicios --yes

    REQUISITOS PREVIOS
      - Azure CLI instalada y sesion iniciada (az login)
      - Docker Desktop arrancado (las imagenes se construyen en local: ACR Tasks
        esta bloqueado en suscripciones de credito de prueba)
      - dotnet-ef instalado (dotnet tool install --global dotnet-ef)
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Sufijo,

    [Parameter(Mandatory = $true)]
    [string]$RemitenteEmail,

    [Parameter(Mandatory = $true)]
    [string]$DestinatarioEmail,

    [string]$Grupo = "rg-demo-microservicios",
    [string]$Region = "spaincentral",

    # Recurso de Communication Services creado a mano (ver seccion 0)
    [string]$NombreAcs = "acs-demo-alg-7421"
)

$ErrorActionPreference = "Stop"

# Nombres derivados del sufijo
$servidorSql = "sql-demo-$Sufijo"
$namespaceSb = "sb-demo-$Sufijo"
$registro    = "acrdemo" + ($Sufijo -replace '-', '')
$entorno     = "env-demo-microservicios"
$servidorRegistro = "$registro.azurecr.io"

# Identidad de quien ejecuta: sera el administrador de Entra del servidor SQL.
$adminSid    = az ad signed-in-user show --query id -o tsv
$adminNombre = az ad signed-in-user show --query userPrincipalName -o tsv
$suscripcion = az account show --query id -o tsv

Write-Host "`n=== 0. REQUISITO MANUAL ===" -ForegroundColor Yellow
Write-Host "Antes de continuar debe existir un recurso de Communication Services"
Write-Host "llamado '$NombreAcs' con un Email Communication Service y un dominio"
Write-Host "gestionado de Azure conectado. Se crea por portal."
Read-Host "Pulsa Enter cuando este listo"

Write-Host "`n=== 1. Proveedores de recursos ===" -ForegroundColor Cyan
# Una suscripcion nueva no trae todos los servicios habilitados. Sin esto,
# la creacion falla con MissingSubscriptionRegistration.
$proveedores = @(
    "Microsoft.Sql", "Microsoft.ContainerRegistry", "Microsoft.App",
    "Microsoft.OperationalInsights", "Microsoft.ServiceBus",
    "Microsoft.Insights", "Microsoft.Communication"
)
foreach ($p in $proveedores) { az provider register --namespace $p | Out-Null }
az provider register --namespace Microsoft.Sql --wait | Out-Null

Write-Host "`n=== 2. Grupo de recursos ===" -ForegroundColor Cyan
az group create --name $Grupo --location $Region | Out-Null

Write-Host "`n=== 3. SQL Server sin contrasena ===" -ForegroundColor Cyan
# El servidor se crea directamente con autenticacion solo por Entra: no llega a
# existir ninguna contrasena de administrador de SQL.
# Tier Basic (~5 USD/mes por base, facturado por hora).
#   Descartado el serverless con pausa automatica: el delivery service del outbox
#   y la limpieza del inbox consultan la base cada pocos segundos, asi que nunca
#   se pausaria y saldria mas caro.
#   Descartada la oferta gratuita de Azure SQL: no esta disponible en Spain Central.
az sql server create `
    --name $servidorSql --resource-group $Grupo --location $Region `
    --enable-ad-only-auth `
    --external-admin-principal-type User `
    --external-admin-name $adminNombre `
    --external-admin-sid $adminSid | Out-Null

# 0.0.0.0 es un valor especial: NO significa "internet entero", significa
# "recursos de Azure". Aun asi es amplio: incluye los de otros clientes.
# La alternativa correcta serian endpoints privados.
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
function CadenaSql($base) {
    "Server=tcp:$hostSql,1433;Database=$base;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

Write-Host "`n=== 4. Migraciones de EF Core ===" -ForegroundColor Cyan
# Se conectan con la identidad de quien ejecuta el script, que es el
# administrador de Entra del servidor. Sin contrasena.
dotnet ef database update `
    --project src/Pedidos/Pedidos.Infrastructure `
    --startup-project src/Pedidos/Pedidos.Api `
    --connection (CadenaSql "PedidosDb")

dotnet ef database update `
    --project src/Inventario/Inventario.Api `
    --connection (CadenaSql "InventarioDb")

Write-Host "`n=== 5. Service Bus ===" -ForegroundColor Cyan
# Standard es obligatorio: Basic solo ofrece colas, y el fan-out necesita
# topics con suscripciones. ~10 USD/mes de tarifa base.
az servicebus namespace create `
    --name $namespaceSb --resource-group $Grupo --location $Region --sku Standard | Out-Null

Write-Host "`n=== 6. Container Registry ===" -ForegroundColor Cyan
# ~5 USD/mes, facturado por dia aunque no contenga ninguna imagen.
az acr create --name $registro --resource-group $Grupo --location $Region --sku Basic | Out-Null

Write-Host "`n=== 7. Construir y subir imagenes ===" -ForegroundColor Cyan
# Se construyen en local porque Microsoft bloquea ACR Tasks en suscripciones
# financiadas con credito de prueba (error TasksOperationsNotAllowed).
az acr login --name $registro

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
# de EF a nivel Information son ruido caro. Se leen con 'az containerapp logs show'.
az extension add --name containerapp --upgrade | Out-Null
az containerapp env create `
    --name $entorno --resource-group $Grupo --location $Region `
    --logs-destination none | Out-Null

Write-Host "`n=== 9. Container Apps ===" -ForegroundColor Cyan
# min-replicas 1: son consumidores de mensajes. A cero no consumirian nada,
#   y sin una regla KEDA nada podria despertarlos.
# max-replicas 1: bloquea el escalado automatico a proposito. Al escalar por
#   encima del minimo, TODAS las replicas pasan a tarifa activa (8x mas cara).
#   Medido: con esta configuracion las tres se facturan siempre en reposo.
# --registry-identity system: crea la identidad administrada y le da permiso
#   para leer del registro. Esa misma identidad se usa despues para SQL,
#   Service Bus y Communication Services.
# NINGUNA cadena de conexion lleva credenciales: todas autentican por identidad.

$endpointAcs = "https://" + (az communication show --name $NombreAcs --resource-group $Grupo --query hostName -o tsv) + "/"

az containerapp create `
    --name ca-inventario --resource-group $Grupo --environment $entorno `
    --image "$servidorRegistro/inventario-api:v1" `
    --registry-server $servidorRegistro --registry-identity system `
    --target-port 8080 --ingress external `
    --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 `
    --env-vars "ASPNETCORE_ENVIRONMENT=Development" `
               "ConnectionStrings__InventarioDb=$(CadenaSql 'InventarioDb')" | Out-Null

az containerapp create `
    --name ca-pedidos --resource-group $Grupo --environment $entorno `
    --image "$servidorRegistro/pedidos-api:v1" `
    --registry-server $servidorRegistro --registry-identity system `
    --target-port 8080 --ingress external `
    --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 `
    --env-vars "ASPNETCORE_ENVIRONMENT=Development" `
               "ConnectionStrings__PedidosDb=$(CadenaSql 'PedidosDb')" | Out-Null

# Sin ingress ni target-port: es un worker, no escucha HTTP.
# Su Dockerfile usa la imagen base 'runtime' en vez de 'aspnet' por lo mismo.
az containerapp create `
    --name ca-notificaciones --resource-group $Grupo --environment $entorno `
    --image "$servidorRegistro/notificaciones-worker:v1" `
    --registry-server $servidorRegistro --registry-identity system `
    --cpu 0.25 --memory 0.5Gi --min-replicas 1 --max-replicas 1 `
    --env-vars "Email__Endpoint=$endpointAcs" `
               "Email__Remitente=$RemitenteEmail" `
               "Email__Destinatario=$DestinatarioEmail" | Out-Null

Write-Host "`n=== 10. Roles RBAC para las identidades ===" -ForegroundColor Cyan
# Las identidades solo existen despues de crear las apps, por eso esta seccion
# va aqui y no antes.
$identidades = @{}
foreach ($app in @("ca-inventario", "ca-pedidos", "ca-notificaciones")) {
    $identidades[$app] = az containerapp show --name $app --resource-group $Grupo --query identity.principalId -o tsv
}

$ambitoSb = "/subscriptions/$suscripcion/resourceGroups/$Grupo/providers/Microsoft.ServiceBus/namespaces/$namespaceSb"

# Los tres necesitan Sender ademas de Receiver, incluido Notificaciones, que solo
# consume: cuando un consumer falla, MassTransit escribe en la cola _error y
# publica un evento Fault, y ambas cosas son envios.
foreach ($app in $identidades.Keys) {
    foreach ($rol in @("Azure Service Bus Data Sender", "Azure Service Bus Data Receiver")) {
        az role assignment create `
            --assignee-object-id $identidades[$app] --assignee-principal-type ServicePrincipal `
            --role $rol --scope $ambitoSb | Out-Null
    }
}

# Communication Services. Rol mas amplio del necesario: el conjunto minimo de
# permisos para enviar correo no esta claramente documentado por Microsoft.
# Deuda consciente, a diferencia de SQL y Service Bus, que si usan roles acotados.
$ambitoAcs = "/subscriptions/$suscripcion/resourceGroups/$Grupo/providers/Microsoft.Communication/CommunicationServices/$NombreAcs"
az role assignment create `
    --assignee-object-id $identidades["ca-notificaciones"] --assignee-principal-type ServicePrincipal `
    --role "Communication and Email Service Owner" --scope $ambitoAcs | Out-Null

Write-Host "`n=== 11. Deshabilitar autenticacion por clave ===" -ForegroundColor Cyan
# Esto es lo que convierte "no uso contrasenas" en "no existen contrasenas".
# El servidor SQL ya se creo con --enable-ad-only-auth.
# Communication Services no expone esta propiedad: es una diferencia entre
# servicios que conviene conocer.
az servicebus namespace update `
    --name $namespaceSb --resource-group $Grupo --disable-local-auth true | Out-Null

Write-Host "`n=== 12. PASO MANUAL: usuarios de base de datos ===" -ForegroundColor Yellow
Write-Host "Conectate al editor de consultas del portal (autenticacion de Entra)"
Write-Host "y ejecuta en cada base:`n"
Write-Host "  -- En InventarioDb:"
Write-Host "  CREATE USER [ca-inventario] FROM EXTERNAL PROVIDER;"
Write-Host "  ALTER ROLE db_datareader ADD MEMBER [ca-inventario];"
Write-Host "  ALTER ROLE db_datawriter ADD MEMBER [ca-inventario];`n"
Write-Host "  -- En PedidosDb:"
Write-Host "  CREATE USER [ca-pedidos] FROM EXTERNAL PROVIDER;"
Write-Host "  ALTER ROLE db_datareader ADD MEMBER [ca-pedidos];"
Write-Host "  ALTER ROLE db_datawriter ADD MEMBER [ca-pedidos];`n"
Write-Host "Notificaciones no recibe acceso a ninguna base: no tiene DbContext."
Write-Host "Los permisos son de lectura y escritura de datos, NO de modificar el"
Write-Host "esquema: las migraciones las ejecuta el administrador, no la aplicacion."
Read-Host "Pulsa Enter cuando lo hayas ejecutado"

Write-Host "`n=== 13. Reiniciar para aplicar permisos ===" -ForegroundColor Cyan
foreach ($app in @("ca-inventario", "ca-pedidos", "ca-notificaciones")) {
    $revision = az containerapp show --name $app --resource-group $Grupo --query properties.latestRevisionName -o tsv
    az containerapp revision restart --name $app --resource-group $Grupo --revision $revision | Out-Null
}

Write-Host "`n=== Listo ===" -ForegroundColor Green
az containerapp list --resource-group $Grupo `
    --query "[].{app:name, url:properties.configuration.ingress.fqdn}" -o table

Write-Host "`nComprobacion: ninguna app debe tener secretos." -ForegroundColor Green
foreach ($app in @("ca-inventario", "ca-pedidos", "ca-notificaciones")) {
    $secretos = az containerapp secret list --name $app --resource-group $Grupo --query "[].name" -o tsv
    if ($secretos) { Write-Host "  $app : $secretos" } else { Write-Host "  $app : ninguno" }
}

Write-Host "`nPara destruirlo todo: az group delete --name $Grupo --yes" -ForegroundColor Yellow
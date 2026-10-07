# Un sistema sin credenciales

Este documento recoge cómo se autentica el sistema en Azure y qué se puede comprobar de forma objetiva. Lo escribo porque "no tengo secretos en el código" es una frase que dice todo el mundo, y casi siempre significa que los secretos están en otro sitio.

Aquí no hay secretos en el código, ni en variables de entorno, ni en un almacén. Las credenciales no existen.

## La idea

Cada uno de los tres servicios tiene una identidad administrada asignada por el sistema. Es una identidad en Microsoft Entra que Azure crea y gestiona por su cuenta, sin contraseña asociada.

Cuando el servicio necesita hablar con Azure SQL, con Service Bus o con Communication Services, no envía una contraseña. Pide un token a la plataforma, y la plataforma se lo da porque sabe qué contenedor está preguntando: lo está ejecutando ella. Ese token caduca en una hora y se renueva solo.

Puesto en corto: en vez de que la aplicación demuestre quién es, el entorno responde por ella.

Lo mismo vale para los tres clientes, que vienen de SDK distintos y no se conocen entre sí:

| Qué | Cómo autentica |
|---|---|
| Azure SQL | `Authentication=Active Directory Default` en la cadena de conexión |
| Service Bus | `cfg.Host(new Uri("sb://..."))` en MassTransit, sin credenciales |
| Communication Services | `new EmailClient(endpoint, new DefaultAzureCredential())` |
| Container Registry | `--registry-identity system` al crear la Container App |

Los cuatro acaban en `DefaultAzureCredential`, que es la pieza común. Por eso la identidad administrada es un concepto y no un truco de un servicio concreto.

## Las pruebas

### Ninguna aplicación guarda secretos

```
ca-inventario     : ninguno
ca-pedidos        : ninguno
ca-notificaciones : ninguno
```

Container Apps permite guardar valores cifrados a nivel de aplicación, y durante la migración los usé para las cadenas de conexión. Ya no hay ninguno.

### Service Bus rechaza las claves compartidas

```
Namespace         ClavesDeshabilitadas
----------------  ----------------------
sb-demo-...       True
```

Esto es lo que separa "no uso la clave" de "la clave no sirve". El namespace se creó con autenticación por clave habilitada, que es el valor por defecto, y la desactivé una vez verificado que la identidad administrada funcionaba. A partir de ahí, la cadena `Endpoint=sb://...;SharedAccessKey=...` dejó de abrir nada, también para quien la tuviera guardada de antes.

### SQL Server solo admite Entra

```
AzureAdOnlyAuthentication    Name     ResourceGroup
---------------------------  -------  ----------------------
True                         Default  rg-demo-microservicios
```

El servidor tenía un administrador clásico de SQL con contraseña. Ya no: el inicio de sesión por usuario y contraseña está deshabilitado en el servidor entero, incluido el de administrador.

Consecuencia práctica para mí: las migraciones de EF Core también se ejecutan con mi identidad de Azure CLI, no con una contraseña.

```
dotnet ef database update --connection "Server=tcp:...;Database=PedidosDb;Authentication=Active Directory Default;Encrypt=True;..."
```

### Cada servicio tiene solo los permisos que usa

```
--- ca-pedidos ---
AcrPull                           .../registries/acrdemo...
Azure Service Bus Data Sender     .../namespaces/sb-demo-...
Azure Service Bus Data Receiver   .../namespaces/sb-demo-...
Monitoring Metrics Publisher      .../components/appi-demo-microservicios

--- ca-inventario ---
AcrPull                           .../registries/acrdemo...
Azure Service Bus Data Sender     .../namespaces/sb-demo-...
Azure Service Bus Data Receiver   .../namespaces/sb-demo-...
Monitoring Metrics Publisher      .../components/appi-demo-microservicios

--- ca-notificaciones ---
AcrPull                                 .../registries/acrdemo...
Azure Service Bus Data Sender           .../namespaces/sb-demo-...
Azure Service Bus Data Receiver         .../namespaces/sb-demo-...
Communication and Email Service Owner   .../CommunicationServices/acs-demo-...
Monitoring Metrics Publisher            .../components/appi-demo-microservicios
```

Tres cosas que vale la pena señalar de esta lista.

**`Monitoring Metrics Publisher` permite escribir telemetría, no leerla.** Los servicios mandan datos a Application Insights y no pueden consultarlos. Quien consulta soy yo, desde el portal, con mi propia identidad.

**Notificaciones necesita `Data Sender` aunque solo consuma.** Esto no es obvio y lo descubrí razonándolo antes de que fallara: cuando un consumer lanza una excepción, MassTransit escribe el mensaje en una cola `_error` y publica un evento `Fault`. Las dos cosas son envíos. Con solo `Data Receiver`, el primer fallo dejaría el mensaje atascado en vez de apartarlo. La lección general es que los permisos no se deducen de lo que hace tu código, sino de lo que hace tu código más lo que hace el framework por debajo.

**El acceso a SQL no aparece aquí, y es correcto.** Azure tiene dos sistemas de permisos distintos y se confunden a menudo. El control de acceso de Azure (RBAC) gobierna los recursos: crear, borrar, configurar, y en algunos servicios también el plano de datos. Los permisos dentro de una base de datos SQL no viven ahí, viven en la propia base:

```sql
CREATE USER [ca-inventario] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [ca-inventario];
ALTER ROLE db_datawriter ADD MEMBER [ca-inventario];
```

`FROM EXTERNAL PROVIDER` significa que el usuario no tiene contraseña: la base le pregunta a Entra quién es. Y los roles que le doy son de leer y escribir datos, no de modificar el esquema. Las migraciones las ejecuto yo como administrador, la aplicación no puede crear ni alterar tablas.

Notificaciones no tiene usuario en ninguna base porque no tiene `DbContext`. Mínimo privilegio real, no teórico.

## Por qué no hay Key Vault

El plan que seguí pedía crear un Key Vault y mover ahí las cadenas de conexión. No lo hice, y creo que es la decisión correcta.

Las dos cosas que pedía el plan se anulan entre sí: si la identidad administrada funciona, no quedan cadenas de conexión que guardar. Montar un Key Vault para dejarlo vacío, o para meter dentro algo que ya no es un secreto, no aporta seguridad. Aporta una línea en el CV.

Key Vault lo necesitaría si tuviera secretos que no pueden sustituirse por identidad: la clave de una API de terceros, un certificado, credenciales de un sistema que no habla con Entra. No es el caso.

Saber para qué sirve una herramienta incluye saber cuándo no hace falta.

## Lo que no está resuelto

Tres cosas que un sistema real tendría que arreglar y este no.

**El rol de Communication Services es más amplio del necesario.** `Communication and Email Service Owner` da acceso a todas las operaciones del servicio, no solo a enviar correo. Intenté acotarlo y me encontré con que Microsoft no documenta con claridad cuál es el conjunto mínimo de permisos: hay respuestas contradictorias en sus propios foros. Elegí el rol integrado por pragmatismo, a diferencia de SQL y Service Bus, que sí usan roles acotados. Es la deuda que más me molesta de las tres.

**El firewall de SQL permite conexiones desde cualquier recurso de Azure.** La regla `0.0.0.0` no significa "internet entero", significa "servicios de Azure", pero eso incluye los de otros clientes. La seguridad real la da la identidad, no el firewall. La solución correcta serían endpoints privados o integración con red virtual.

**Las aplicaciones corren con `ASPNETCORE_ENVIRONMENT=Development`.** Eso expone la página de excepciones detalladas en una URL pública, con trazas de pila y rutas internas. Lo mantengo porque así se ve Swagger, que para una demo tiene sentido. En cualquier otro contexto sería un fallo de libro, y lo sé porque me pasó: cuando fallaba una cadena de conexión, la URL pública me devolvió la traza completa con los nombres de mis clases y las rutas del contenedor.

## Lo que me costó llegar aquí

El camino no fue limpio y las partes que fallaron enseñan más que las que salieron a la primera.

Intenté primero el relé SMTP de Communication Services, que habría permitido no tocar el código de envío de correo. El formulario del portal para crear el usuario SMTP fallaba en silencio: ni error en pantalla, ni notificación, ni nada en el registro de actividad, con la asignación de rol correctamente creada y verificada por CLI. Es una funcionalidad en vista previa. Lo abandoné tras casi una hora, y la decisión fue acertada por un motivo que no era el evidente: ese camino **no admite identidades administradas**, solo aplicaciones registradas con secreto. Habría peleado para llegar a una solución que de todos modos había que cambiar.

Al pasar a Service Bus por identidad, predije que los roles acotados no bastarían para que MassTransit creara la topología al arrancar. Me equivoqué, funcionó. Pero con un matiz que mantengo documentado: lo probé sobre una topología que ya existía. No está verificado que esos roles basten en un namespace vacío, y si alguien despliega esto desde cero puede encontrarse con un fallo de autorización al arrancar. La salida sería asignar `Azure Service Bus Data Owner` temporalmente, dejar que se cree la topología, y volver a los roles acotados.

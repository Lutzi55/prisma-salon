# Prisma Salón

Sistema web de **planificación y contratación de eventos** para un salón: catálogo de paquetes personalizables, seña obligatoria para reservar la fecha, contratos con firma electrónica, cobros con comprobante o con turno en el salón, calendario de disponibilidad, auditoría de cada operación, opiniones de clientes y un asistente con inteligencia artificial.

Trabajo Final Integrador · Programación Web · ASP.NET Core MVC

> **Sitio publicado:** http://prismasalon.runasp.net
> **Autora:** Lucía Gross

---

## Credenciales de prueba

| Rol | Correo | Contraseña |
| --- | --- | --- |
| Administrador | admin@eventos.com | Admin123! |
| Cliente de prueba 1 | cliente@eventos.com | Cliente123 |

El cliente de prueba ya tiene contratos, pagos en distintos estados y un evento pasado para dejar una opinión. También se puede crear una cuenta nueva desde *Registrarse*.

---

## Funcionalidades

**Visitante (sin cuenta)**
- Inicio con paquetes, proceso de contratación y opiniones de clientes (promedio de estrellas).
- Detalle de cada paquete con galería. Precios, horarios, coordinador y opciones **solo para usuarios registrados** (se filtra en el servidor).
- *Disponibilidad*: calendario mensual con los días libres y ocupados, sin datos de otros clientes.
- Registro e inicio de sesión en español.
- Asistente con IA (sin acceso a precios).

**Cliente**
- *Personalizar y pedir* un paquete: fecha, opciones con el total calculado en vivo y pago para reservar.
- **Seña obligatoria del 30 %** (o el total de una vez), solo por transferencia o tarjeta de débito, avisada en todos los paquetes antes de continuar.
- Pantalla *Confirmá tu seña* con monto, tipo y medio; botón para generar un **comprobante de prueba** (imagen marcada como simulación).
- *Mis contratos*: barra de etapas, lectura y **firma electrónica** del contrato (firma dibujada + huella SHA-256 del texto), descarga en PDF.
- Pagos: lo pendiente con su vencimiento, envío o reenvío de comprobantes, pagos informados y estado de cada uno (en revisión, confirmado, rechazado, reembolsado).
- Saldo en **efectivo o cheque con turno** en el salón (lunes a viernes de 10 a 18).
- *Mis opiniones*: califica con estrellas y comenta sus eventos ya realizados.
- *Mi perfil*: datos, contraseña y correo.
- Asistente con IA que conoce **sus** contratos y saldos.

**Administrador**
- ABM de clientes, paquetes, categorías y opciones, shows, coordinadores y empleados.
- Contratos agrupados por lo que hay que hacer ahora, con el **próximo paso** de cada uno; ficha con lo contratado (paquete + opciones con precio congelado), el contrato y los pagos.
- Redacción del contrato desde una plantilla y envío al cliente.
- Pagos: enviar recibo (seña o saldo), confirmar o rechazar comprobantes, registrar cobros en el salón, reprogramar, cancelar y reembolsar, con reglas validadas en el servidor.
- **Calendario** con eventos, horarios, personal, shows y turnos de cobro, y **agenda del personal** para imprimir o copiar por mensaje (el personal no tiene cuenta).
- Reseñas: responder, ocultar o mostrar opiniones.
- **Auditoría**: quién hizo qué y cuándo, en lenguaje natural y con los cambios campo por campo.
- `/Chat/Prueba`: diagnóstico del asistente (conexión con Gemini, modelos disponibles y tiempos de respuesta).

---

## Tecnologías

- ASP.NET Core MVC (.NET 8) + Razor Pages para Identity
- Entity Framework Core + SQL Server (Code First con migraciones)
- ASP.NET Core Identity con roles **Administrador** y **Cliente**
- Bootstrap 5, jQuery Validation Unobtrusive, SweetAlert2
- QuestPDF (contratos y comprobantes en PDF, licencia Community)
- Google Gemini API (asistente con IA) mediante `HttpClient`

---

## Base de datos

14 tablas propias además de las de Identity:

`Clientes`, `Coordinadores`, `Paquetes`, `Shows`, `Empleados`, `Contratos`, `ContratosShows`, `ContratosEmpleados`, `CategoriasOpcion`, `Opciones`, `ContratosOpciones`, `Pagos`, `Resenas`, `Auditorias`.

- Relaciones definidas con Fluent API; claves compuestas en las tablas intermedias (`ContratosShows`, `ContratosEmpleados`, `ContratosOpciones`).
- `DeleteBehavior.Restrict` en todas las relaciones: no se borra en cascada; si hay registros asociados se muestra un mensaje.
- `Auditorias` se relaciona con `AspNetUsers` (el usuario que hizo el cambio); si se borra la cuenta, el registro se conserva con el correo.
- Una reseña por contrato (índice único).
- `Pagos` guarda si es la seña, el medio, el vencimiento, el turno para pagar en el salón y el comprobante.

El diagrama entidad-relación (DER) y el diagrama de casos de uso están incluidos en el repositorio.

---

## Decisiones de diseño

- **ViewModels** en los formularios complejos (personalizar paquete, contrato, pagos, firma, reseñas): validan solo lo que el formulario envía y evitan el *overposting*.
- **Etapa del contrato calculada** (`EtapaContrato`): el próximo paso se deduce de los datos (pagos en revisión, firma, saldo), no de un estado cargado a mano.
- **Reglas de cobro en un solo lugar** (`PoliticaPagos`): porcentaje y medios de la seña, plazos (seña a 7 días, saldo 10 días antes del evento) y horario de turnos.
- **Auditoría automática** con un `SaveChangesInterceptor`: registra cada alta, edición y baja sin depender de cada controlador.
- **Archivos privados** en `App_Data` (fuera de `wwwroot`): se entregan solo a través de un controlador que verifica que el usuario sea el dueño.
- **Precio congelado**: cada opción guarda el precio acordado al contratar; los cambios de precio futuros no alteran contratos firmados.
- **Firma electrónica simple** (no es firma digital con certificado de la Ley 25.506): guarda la imagen de la firma, el nombre, la fecha y la huella SHA-256 del texto firmado. Si el texto cambia, la huella deja de coincidir.
- **IA con datos reales y visibilidad en el servidor**: el contexto que recibe Gemini se arma según el rol; a un visitante nunca se le envían precios. La clave nunca llega al navegador. El sistema consulta qué modelos tiene disponibles la clave y, si uno no responde a tiempo, pasa al siguiente. Límite de 10 consultas por minuto.
- **Claves de protección de datos persistidas** en `App_Data/claves`: los reinicios del hosting no cierran sesiones ni invalidan formularios abiertos.

---

## Cómo ejecutarlo localmente

**Requisitos:** Visual Studio 2022 con el SDK de .NET 8 y SQL Server LocalDB (o SQL Server Express).

1. Clonar el repositorio y abrir la solución.
2. Revisar la cadena `DefaultConnection` en `appsettings.json` (por defecto LocalDB).
3. Configurar la clave de Gemini (gratuita en https://aistudio.google.com/apikey) con **User Secrets**: clic derecho en el proyecto › *Administrar secretos de usuario*:
   ```json
   { "Gemini": { "ApiKey": "TU_CLAVE" } }
   ```
4. Ejecutar (F5). Al iniciar, la aplicación **aplica las migraciones** y carga roles, usuarios de prueba y datos de demostración.

Comandos útiles (Consola del Administrador de paquetes): `Add-Migration Nombre`, `Update-Database`, `Drop-Database`.

---

## Publicación

El sitio está publicado en **MonsterASP.NET** (plan gratuito) con Web Deploy desde Visual Studio. Las migraciones se aplican solas al iniciar la aplicación.

- Los secretos del servidor (cadena de conexión y clave de Gemini) están en `appsettings.Production.json`, que **no se sube al repositorio** (ver `.gitignore` y la plantilla `appsettings.Ejemplo.json`).
- Al publicar se excluye `App_Data` y no se borran los archivos del servidor, para conservar comprobantes, firmas y contratos.
- El plan gratuito no incluye HTTPS, por eso el navegador muestra el sitio como "no seguro".

# Prisma Salón

Sistema web de **planificación y contratación de eventos** para un salón: catálogo de paquetes personalizables, contratos con firma electrónica, cobros con comprobante, auditoría de cada operación, opiniones de clientes y un asistente con inteligencia artificial.

Trabajo Final Integrador · Programación Web · ASP.NET Core MVC

> **Sitio publicado:** https://TU-SITIO.smarterasp.net
> **Autora:** [Nombre y apellido]

---

## Credenciales de prueba

| Rol | Correo | Contraseña |
| --- | --- | --- |
| Administrador | admin@eventos.com | Admin123! |
| Cliente | cliente@eventos.com | Cliente123! |

El cliente de prueba ya tiene contratos, pagos en distintos estados y un evento pasado para dejar una opinión.

---

## Funcionalidades

**Visitante (sin cuenta)**
- Inicio con paquetes, proceso de contratación y opiniones de clientes (promedio de estrellas).
- Detalle de cada paquete con galería. Precios, horarios, coordinador y opciones **solo para usuarios registrados** (se filtra en el servidor).
- Registro e inicio de sesión en español.
- Asistente con IA (sin acceso a precios).

**Cliente**
- *Mis contratos*: lectura y **firma electrónica** del contrato (firma dibujada + huella SHA-256 del texto), descarga en PDF.
- Pagos: ve lo que tiene que pagar con su vencimiento, envía o reenvía comprobantes, informa pagos, sigue el estado (en revisión, confirmado, rechazado, reembolsado).
- *Mis opiniones*: califica con estrellas y comenta sus eventos ya realizados.
- *Mi perfil*: datos, contraseña y correo.
- Asistente con IA que conoce **sus** contratos y saldos.

**Administrador**
- ABM de clientes, paquetes, categorías y opciones, shows, coordinadores y empleados.
- Contratos: paquete + opciones (precio congelado al contratar), shows, personal con hora de citación, redacción desde plantilla y envío al cliente.
- Pagos: solicitar, confirmar, rechazar, reprogramar, cancelar y reembolsar, con reglas de transición validadas en el servidor.
- Reseñas: ocultar o mostrar opiniones.
- **Auditoría**: quién hizo qué y cuándo, en lenguaje natural y con los cambios campo por campo.
- Asistente con IA que arma un resumen de gestión (próximos eventos, comprobantes a revisar, pagos vencidos).

---

## Tecnologías

- ASP.NET Core MVC (.NET 8) + Razor Pages para Identity
- Entity Framework Core + SQL Server (migraciones)
- ASP.NET Core Identity con roles **Administrador** y **Cliente**
- Bootstrap 5, jQuery Validation Unobtrusive, SweetAlert2
- QuestPDF (contratos y comprobantes en PDF, licencia Community)
- Google Gemini API (asistente con IA) mediante `HttpClient`

---

## Base de datos

14 tablas propias además de las de Identity:

`Clientes`, `Coordinadores`, `Paquetes`, `Shows`, `Empleados`, `Contratos`, `ContratosShows`, `ContratosEmpleados`, `CategoriasOpcion`, `Opciones`, `ContratosOpciones`, `Pagos`, `Resenas`, `Auditorias`.

- Claves compuestas en las tablas intermedias (`ContratosShows`, `ContratosEmpleados`, `ContratosOpciones`).
- `DeleteBehavior.Restrict` en todas las relaciones: no se borra en cascada; si hay registros asociados se muestra un mensaje.
- Una reseña por contrato (índice único).

---

## Decisiones de diseño

- **ViewModels** en los formularios complejos (contrato, pagos, firma, reseñas): validan solo lo que el formulario envía y evitan el *overposting*.
- **Auditoría automática** con un `SaveChangesInterceptor`: registra cada alta, edición y baja sin depender de cada controlador.
- **Archivos privados** en `App_Data` (fuera de `wwwroot`): se entregan solo a través de un controlador que verifica que el usuario sea el dueño.
- **Precio congelado**: cada opción guarda el precio acordado al contratar; los cambios de precio futuros no alteran contratos firmados.
- **Firma electrónica simple** (no es firma digital con certificado de la Ley 25.506): guarda la imagen de la firma, el nombre, la fecha y la huella SHA-256 del texto firmado. Si el texto cambia, la huella deja de coincidir.
- **IA con datos reales y visibilidad en el servidor**: el contexto que recibe Gemini se arma según el rol; a un visitante nunca se le envían precios. La clave de la API nunca llega al navegador.

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

El sitio está publicado en SmarterASP.NET. Los secretos del servidor (cadena de conexión y clave de Gemini) están en `appsettings.Production.json`, que **no se sube al repositorio** (ver `.gitignore` y la plantilla `appsettings.Ejemplo.json`).

---

## Créditos

- Fotografías: [completar fuente, por ejemplo Unsplash / Pexels].
- Tipografías Marcellus y Jost (Google Fonts).

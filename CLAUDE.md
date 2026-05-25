# EDInventory — Instrucciones para Claude Code

## Proyecto
Sistema de inventario IT interno para el Departamento de Ingeniería de Diagnostika S.A.
Dos divisiones: **TI** (equipos, hospitales, licitaciones) y **Servicio** (repuestos/líneas).

## Stack
- ASP.NET Core 8 MVC
- Entity Framework Core + Pomelo (MySQL)
- Bootstrap 5 + Bootstrap Icons
- BCrypt.Net-Next (contraseñas — nunca MD5)
- ClosedXML (exportación Excel)
- JsBarcode CDN (barcodes SVG, sin dependencia en proyecto)

## Base de datos
- Motor: MySQL
- Schema: `DB_EInventory`
- Password: `DevWork26#`
- Conexión: configurada en `appsettings.json`

## Estructura del proyecto
```
Controllers/        → AuthController, CatalogController, AdminController,
                      EquipmentController, EngineeringController,
                      HospitalController, GestionController, DashboardController
Models/
  Entities/         → Clases EF Core (una por tabla)
  ViewModels/       → ViewModels para formularios y vistas
  AppRoles.cs       → Constantes de roles (usar siempre estas, no strings literales)
Data/
  AppDbContext.cs   → DbContext principal
Views/              → Vistas Razor por controlador
wwwroot/            → CSS, JS, librerías cliente
```

## Autenticación y roles
- Autenticación por **cookies** (no JWT)
- 7 roles fijos definidos en `AppRoles.cs` y en `TB_UROLES`:
  - `Administrador` (global)
  - `TI.Administrador`, `TI.Tecnico`, `TI.Consulta`
  - `Servicio.Administrador`, `Servicio.Tecnico`, `Servicio.Consulta`
- Usar siempre las constantes de `AppRoles` en los atributos `[Authorize]`
- Grupos de permisos: `TiRead/TiWrite/TiManage`, `SvcRead/SvcWrite/SvcManage`, `AdmRead/AdmWrite/AdmManage`

## Convenciones de código
- Nombres de tablas: prefijo `TB_` en mayúsculas (ej. `TB_ITEQUIP`, `TB_USER`)
- Nombres de columnas en BD: `SNAKE_CASE` en mayúsculas
- Clases C#: PascalCase
- No agregar comentarios obvios — solo cuando el WHY no es evidente
- No agregar manejo de errores para escenarios imposibles
- No crear abstracciones innecesarias — implementar lo que se pide

## Módulos completados (no reimplementar)
1. Setup + DbContext + Auth (login por cookie, dashboard base)
2. Catálogos (Company, Site, Department, AssetTypes, Brand, Model)
3. Empleados y Usuarios
4. Hospitales (ProcessingUnit, Hospital, HospitalDepartment, HospitalRole, HospitalDoctor)
5. Inventario equipos IT (ItEquip, Licitaciones, alertas de vencimiento)
6. Dashboard con estadísticas, alertas y reporte imprimible
7. Bodegas TI + Ubicación extendida + Historial + Escáner + Etiquetas Zebra GK420D (4×4 in)
8. Inventario Ingeniería (EngLine, EngBox, EngPart)
9. Sistema de roles fijos por división
10. Dashboard unificado TI + Servicio

## Notas importantes
- La etiqueta imprimible usa `layout: null` (página independiente sin navbar)
- Escáner de movimiento (`/Equipment/MovementStation`) usa `[IgnoreAntiforgeryToken]` + `[FromBody]` — no cambiar
- Historial de movimientos se registra automáticamente al crear equipo y al cambiar ubicación
- Ubicación de equipo es mutuamente excluyente: BODEGA o HOSPITAL (radio selector en formulario)
- Carga de departamentos por hospital es AJAX

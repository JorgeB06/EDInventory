using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel para crear o editar un equipo IT (<see cref="EDInventory.Models.Entities.ItEquip"/>).
    /// Cubre los tres tipos de ubicación mutuamente excluyentes: Hospital, Bodega y Sede.
    /// También transporta <see cref="HistNotes"/> para registrar automáticamente
    /// el cambio de ubicación en el historial al guardar.
    /// </summary>
    public class ItEquipViewModel
    {
        /// <summary>Clave primaria del equipo IT. Cero indica creación.</summary>
        public int ItequipCode { get; set; }

        /// <summary>Descripción del equipo (máx. 100 caracteres).</summary>
        [Required(ErrorMessage = "La descripcion es requerida")]
        [Display(Name = "Descripcion")]
        [StringLength(100)]
        public string ItequipDesc { get; set; } = string.Empty;

        // ── Ubicación: Hospital ────────────────────────────────

        /// <summary>FK al hospital donde está instalado el equipo.</summary>
        [Display(Name = "Hospital")]
        public int? HospCode { get; set; }

        /// <summary>FK al departamento del hospital.</summary>
        [Display(Name = "Zona / Departamento")]
        public int? HospDepCode { get; set; }

        /// <summary>Posición exacta dentro del departamento (máx. 60 caracteres).</summary>
        [Display(Name = "Posicion en zona")]
        [StringLength(60)]
        public string? ItequipHospPos { get; set; }

        // ── Ubicación: Bodega ──────────────────────────────────

        /// <summary>FK a la bodega donde está almacenado el equipo.</summary>
        [Display(Name = "Bodega")]
        public int? WareCode { get; set; }

        /// <summary>Rack o pasillo dentro de la bodega (máx. 30 caracteres).</summary>
        [Display(Name = "Rack")]
        [StringLength(30)]
        public string? WareRack { get; set; }

        /// <summary>Estante dentro del rack (máx. 30 caracteres).</summary>
        [Display(Name = "Estante")]
        [StringLength(30)]
        public string? WareEstante { get; set; }

        /// <summary>Caja o contenedor dentro del estante (máx. 30 caracteres).</summary>
        [Display(Name = "Caja")]
        [StringLength(30)]
        public string? WareCaja { get; set; }

        // ── Ubicación: Sede ────────────────────────────────────

        /// <summary>FK a la sede donde está ubicado el equipo.</summary>
        [Display(Name = "Sede")]
        public int? SiteCode { get; set; }

        // ── Identificación ─────────────────────────────────────

        /// <summary>FK al modelo del equipo.</summary>
        [Required(ErrorMessage = "El modelo es requerido")]
        [Display(Name = "Modelo")]
        public int? ModelCode { get; set; }

        /// <summary>Número de serie del fabricante (máx. 40 caracteres).</summary>
        [Display(Name = "Numero de Serie")]
        [StringLength(40)]
        public string? ItequipSn { get; set; }

        /// <summary>FK a la licitación bajo la que fue adquirido.</summary>
        [Display(Name = "Licitacion")]
        public int? LicCode { get; set; }

        /// <summary>Número interno del equipo asignado por Diagnostika (máx. 25 caracteres).</summary>
        [Display(Name = "Numero de Equipo")]
        [StringLength(25)]
        public string? ItequipNum { get; set; }

        /// <summary>Fecha de inicio de la licitación.</summary>
        [Display(Name = "Inicio de Licitacion")]
        public DateOnly? ItequipDslic { get; set; }

        /// <summary>Fecha de vencimiento de la licitación.</summary>
        [Display(Name = "Fin de Licitacion")]
        public DateOnly? ItequipDelic { get; set; }

        /// <summary>Número o referencia de la garantía del fabricante (máx. 40 caracteres).</summary>
        [Display(Name = "Numero de Garantia")]
        [StringLength(40)]
        public string? ItequipGnum { get; set; }

        /// <summary>Fecha en que el equipo fue dado de baja.</summary>
        [Display(Name = "Fecha Baja Equipo")]
        public DateOnly? ItequipDjequip { get; set; }

        /// <summary>Especificaciones u observaciones adicionales (máx. 200 caracteres).</summary>
        [Display(Name = "Datos Adicionales")]
        [StringLength(200)]
        public string? ItequipAddata { get; set; }

        /// <summary>Fecha de registro del equipo en el sistema.</summary>
        [Display(Name = "Fecha de Registro")]
        public DateOnly? ItequipDnew { get; set; }

        /// <summary>Fecha de la última modificación del registro.</summary>
        [Display(Name = "Ultima Modificacion")]
        public DateOnly? ItequipDmod { get; set; }

        /// <summary>Indica si el equipo está activo en el inventario.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Estado del ciclo de vida del equipo.</summary>
        [Display(Name = "Estado")]
        [StringLength(20)]
        public string EquipStatus { get; set; } = "EN_SERVICIO";

        // ── Responsable ───────────────────────────────────────
        [Display(Name = "Tecnico responsable")]
        public int? ResponsibleUser { get; set; }

        [Display(Name = "Responsable externo")]
        [StringLength(80)]
        public string? ResponsibleExt { get; set; }

        // ── Costo / Depreciacion ──────────────────────────────
        [Display(Name = "Costo de adquisicion")]
        public decimal? AcquireCost { get; set; }

        [Display(Name = "Fecha de adquisicion")]
        public DateOnly? AcquireDate { get; set; }

        [Display(Name = "Vida util (años)")]
        [Range(1, 50)]
        public int? DepreYears { get; set; }

        // ── Red ───────────────────────────────────────────────

        [Display(Name = "Nombre de Host")]
        [StringLength(60)]
        public string? NetHostname { get; set; }

        [Display(Name = "En Dominio")]
        public bool NetInDomain { get; set; }

        [Display(Name = "Conectado a Red")]
        public bool NetEnabled { get; set; }

        [Display(Name = "Direccion IP")]
        [StringLength(45)]
        public string? NetIp { get; set; }

        [Display(Name = "Tipo IP")]
        [StringLength(6)]
        public string? NetType { get; set; }

        // ── Historial de ubicación ─────────────────────────────

        [Display(Name = "Notas del movimiento")]
        [StringLength(200)]
        public string? HistNotes { get; set; }

        // ── Listas desplegables ────────────────────────────────

        /// <summary>Lista de sedes para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Sites { get; set; } = [];

        /// <summary>Lista de hospitales para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];

        /// <summary>Lista de departamentos hospitalarios para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> HospDepartments { get; set; } = [];

        /// <summary>Lista de bodegas para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Warehouses { get; set; } = [];

        /// <summary>Lista de modelos para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Models { get; set; } = [];

        /// <summary>Lista de licitaciones para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Licitaciones { get; set; } = [];

        /// <summary>Lista de tecnicos internos para el selector de responsable.</summary>
        public IEnumerable<SelectListItem> TechUsers { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar una licitación o contrato de mantenimiento.
    /// Controla los umbrales de alerta de vencimiento en el Dashboard.
    /// </summary>
    public class LicitacionViewModel
    {
        /// <summary>Clave primaria de la licitación. Cero indica creación.</summary>
        public int LicCode { get; set; }

        /// <summary>Número oficial de la licitación (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El numero de licitacion es requerido")]
        [Display(Name = "Numero de Licitacion")]
        [StringLength(45)]
        public string LicNum { get; set; } = string.Empty;

        /// <summary>Descripción o nombre del contrato (máx. 60 caracteres).</summary>
        [Display(Name = "Descripcion")]
        [StringLength(60)]
        public string? LicDesc { get; set; }

        /// <summary>Fecha de inicio de vigencia del contrato.</summary>
        [Display(Name = "Fecha Inicio")]
        public DateOnly? LicStart { get; set; }

        /// <summary>Fecha de vencimiento del contrato.</summary>
        [Display(Name = "Fecha Vencimiento")]
        public DateOnly? LicEnd { get; set; }

        /// <summary>Días de anticipación para mostrar la alerta de "por vencer" en el Dashboard (1–3650).</summary>
        [Display(Name = "Dias de aviso previo")]
        [Range(1, 3650)]
        public int LicWarnDays { get; set; } = 180;

        /// <summary>Indica si la licitación vencida fue aplazada administrativamente (alerta amarilla en lugar de roja).</summary>
        [Display(Name = "Aplazada")]
        public bool LicPostponed { get; set; }

        /// <summary>Justificación del aplazamiento (máx. 150 caracteres).</summary>
        [Display(Name = "Nota de aplazamiento")]
        [StringLength(150)]
        public string? LicPostponedNote { get; set; }

        /// <summary>Indica si la licitación está activa en el sistema.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// ViewModel para crear o editar una bodega de almacenamiento.
    /// </summary>
    public class WarehouseViewModel
    {
        /// <summary>Clave primaria de la bodega. Cero indica creación.</summary>
        public int WareCode { get; set; }

        /// <summary>Nombre descriptivo de la bodega (máx. 80 caracteres).</summary>
        [Required(ErrorMessage = "El nombre de la bodega es requerido")]
        [Display(Name = "Nombre de Bodega")]
        [StringLength(80)]
        public string WareName { get; set; } = string.Empty;

        /// <summary>Descripción o ubicación adicional de la bodega (máx. 150 caracteres).</summary>
        [Display(Name = "Descripcion")]
        [StringLength(150)]
        public string? WareDesc { get; set; }

        /// <summary>Indica si la bodega está operativa.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// ViewModel para crear o editar un activo clínico de ingeniería (<see cref="EDInventory.Models.Entities.EngAsset"/>).
    /// Equivalente a <see cref="ItEquipViewModel"/> para la división Servicio.
    /// Incluye campo <see cref="HistNotes"/> para registrar el cambio de ubicación en historial.
    /// </summary>
    public class EngAssetViewModel
    {
        /// <summary>Clave primaria del activo. Cero indica creación.</summary>
        public int AssetCode { get; set; }

        /// <summary>Descripción del activo clínico (máx. 100 caracteres).</summary>
        [Required(ErrorMessage = "La descripcion es requerida")]
        [Display(Name = "Descripcion")]
        [StringLength(100)]
        public string AssetDesc { get; set; } = string.Empty;

        // ── Línea ──────────────────────────────────────────────

        /// <summary>FK a la línea de servicio de ingeniería.</summary>
        [Required(ErrorMessage = "La linea de servicio es requerida")]
        [Display(Name = "Linea de Servicio")]
        public int? LineCode { get; set; }

        // ── Ubicación: Hospital ────────────────────────────────

        /// <summary>FK al hospital donde está instalado el activo.</summary>
        [Display(Name = "Hospital")]
        public int? HospCode { get; set; }

        /// <summary>FK al departamento del hospital.</summary>
        [Display(Name = "Zona / Departamento")]
        public int? HospDepCode { get; set; }

        /// <summary>Posición exacta dentro del departamento (máx. 60 caracteres).</summary>
        [Display(Name = "Posicion en zona")]
        [StringLength(60)]
        public string? AssetHospPos { get; set; }

        // ── Ubicación: Bodega ──────────────────────────────────

        /// <summary>FK a la bodega donde está almacenado el activo.</summary>
        [Display(Name = "Bodega")]
        public int? WareCode { get; set; }

        /// <summary>Rack o pasillo dentro de la bodega (máx. 30 caracteres).</summary>
        [Display(Name = "Rack")]
        [StringLength(30)]
        public string? WareRack { get; set; }

        /// <summary>Estante dentro del rack (máx. 30 caracteres).</summary>
        [Display(Name = "Estante")]
        [StringLength(30)]
        public string? WareEstante { get; set; }

        // ── Ubicación: Sede ────────────────────────────────────

        /// <summary>FK a la sede donde está ubicado el activo.</summary>
        [Display(Name = "Sede")]
        public int? SiteCode { get; set; }

        // ── Identificación ─────────────────────────────────────

        /// <summary>FK al modelo del activo.</summary>
        [Display(Name = "Modelo")]
        public int? ModelCode { get; set; }

        /// <summary>Número de serie del fabricante (máx. 40 caracteres).</summary>
        [Display(Name = "Numero de Serie")]
        [StringLength(40)]
        public string? AssetSN { get; set; }

        /// <summary>FK a la licitación bajo la que fue adquirido.</summary>
        [Display(Name = "Licitacion")]
        public int? LicCode { get; set; }

        /// <summary>Número interno del activo asignado por Diagnostika (máx. 25 caracteres).</summary>
        [Display(Name = "Numero de Activo")]
        [StringLength(25)]
        public string? AssetNum { get; set; }

        /// <summary>Fecha de inicio de la licitación.</summary>
        [Display(Name = "Inicio de Licitacion")]
        public DateOnly? AssetDslic { get; set; }

        /// <summary>Fecha de vencimiento de la licitación.</summary>
        [Display(Name = "Fin de Licitacion")]
        public DateOnly? AssetDelic { get; set; }

        /// <summary>Número o referencia de la garantía del fabricante (máx. 40 caracteres).</summary>
        [Display(Name = "Numero de Garantia")]
        [StringLength(40)]
        public string? AssetGnum { get; set; }

        /// <summary>Fecha en que el activo fue dado de baja.</summary>
        [Display(Name = "Fecha Baja Activo")]
        public DateOnly? AssetDjequip { get; set; }

        /// <summary>Especificaciones u observaciones adicionales (máx. 200 caracteres).</summary>
        [Display(Name = "Datos Adicionales")]
        [StringLength(200)]
        public string? AssetAddata { get; set; }

        /// <summary>Fecha de registro del activo en el sistema.</summary>
        [Display(Name = "Fecha de Registro")]
        public DateOnly? AssetDnew { get; set; }

        /// <summary>Fecha de la última modificación del registro.</summary>
        [Display(Name = "Ultima Modificacion")]
        public DateOnly? AssetDmod { get; set; }

        /// <summary>Indica si el activo está activo en el inventario.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Estado del ciclo de vida del activo.</summary>
        [Display(Name = "Estado")]
        [StringLength(20)]
        public string AssetStatus { get; set; } = "EN_SERVICIO";

        // ── Responsable ───────────────────────────────────────
        [Display(Name = "Tecnico responsable")]
        public int? ResponsibleUser { get; set; }

        [Display(Name = "Responsable externo")]
        [StringLength(80)]
        public string? ResponsibleExt { get; set; }

        // ── Costo / Depreciacion ──────────────────────────────
        [Display(Name = "Costo de adquisicion")]
        public decimal? AcquireCost { get; set; }

        [Display(Name = "Fecha de adquisicion")]
        public DateOnly? AcquireDate { get; set; }

        [Display(Name = "Vida util (años)")]
        [Range(1, 50)]
        public int? DepreYears { get; set; }

        // ── Red ───────────────────────────────────────────────

        [Display(Name = "Nombre de Host")]
        [StringLength(60)]
        public string? NetHostname { get; set; }

        [Display(Name = "En Dominio")]
        public bool NetInDomain { get; set; }

        [Display(Name = "Conectado a Red")]
        public bool NetEnabled { get; set; }

        [Display(Name = "Direccion IP")]
        [StringLength(45)]
        public string? NetIp { get; set; }

        [Display(Name = "Tipo IP")]
        [StringLength(6)]
        public string? NetType { get; set; }

        [Display(Name = "Notas del movimiento")]
        [StringLength(200)]
        public string? HistNotes { get; set; }

        // ── Listas desplegables ────────────────────────────────

        /// <summary>Lista de líneas de servicio para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Lines { get; set; } = [];

        /// <summary>Lista de sedes para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Sites { get; set; } = [];

        /// <summary>Lista de hospitales para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];

        /// <summary>Lista de departamentos hospitalarios para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> HospDepartments { get; set; } = [];

        /// <summary>Lista de bodegas para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Warehouses { get; set; } = [];

        /// <summary>Lista de modelos para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Models { get; set; } = [];

        /// <summary>Lista de licitaciones para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Licitaciones { get; set; } = [];

        /// <summary>Lista de tecnicos internos para el selector de responsable.</summary>
        public IEnumerable<SelectListItem> TechUsers { get; set; } = [];
    }

    /// <summary>
    /// Request DTO para el endpoint de movimiento masivo por escaneo de número de serie.
    /// Permite reubicar un equipo IT o activo clínico indicando solo el serial
    /// y la nueva ubicación (Bodega u Hospital).
    /// </summary>
    public class ScanMoveRequest
    {
        /// <summary>Número de serie del equipo o activo a reubicar.</summary>
        public string? Serial { get; set; }

        /// <summary>
        /// Tipo de ubicación destino. Valores: <c>"BODEGA"</c>, <c>"HOSPITAL"</c>.
        /// </summary>
        public string? LocType { get; set; }

        // ── Bodega ────────────────────────────────────────────

        /// <summary>FK a la bodega destino (requerido si LocType = "BODEGA").</summary>
        public int? WareCode { get; set; }

        /// <summary>Rack o pasillo dentro de la bodega destino.</summary>
        public string? WareRack { get; set; }

        /// <summary>Estante dentro del rack de la bodega destino.</summary>
        public string? WareEstante { get; set; }

        /// <summary>Caja dentro del estante de la bodega destino.</summary>
        public string? WareCaja { get; set; }

        // ── Hospital ──────────────────────────────────────────

        /// <summary>FK al hospital destino (requerido si LocType = "HOSPITAL").</summary>
        public int? HospCode { get; set; }

        /// <summary>FK al departamento del hospital destino.</summary>
        public int? HospDepCode { get; set; }

        /// <summary>Posición exacta dentro del departamento del hospital destino.</summary>
        public string? HospPos { get; set; }

        /// <summary>Notas del cambio de ubicación registradas en el historial.</summary>
        public string? HistNotes { get; set; }
    }
}

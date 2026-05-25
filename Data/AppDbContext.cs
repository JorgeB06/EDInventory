using EDInventory.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDInventory.Data
{
    /// <summary>
    /// Contexto principal de Entity Framework Core para la base de datos MySQL <c>DB_EInventory</c>.
    /// Registra todos los <see cref="DbSet{TEntity}"/> del sistema, organizados en tres módulos:
    /// Administración (empresas, sedes, empleados, usuarios), TI (equipos, licitaciones, bodegas,
    /// mantenimientos) e Ingeniería/Servicio (líneas, cajas, repuestos, activos clínicos).
    /// El método <see cref="OnModelCreating"/> aplica configuraciones adicionales como la longitud
    /// máxima del hash BCrypt de usuario.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>Inicializa el contexto con las opciones de conexión configuradas en Program.cs.</summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── Administración ────────────────────────────────────────────────────

        /// <summary>Tipos de documento de empresa (TB_DOCTYPE).</summary>
        public DbSet<DocumentTypeCompany> DocumentTypeCompanies { get; set; }
        /// <summary>Empresas propietarias de activos (TB_COMPANY).</summary>
        public DbSet<Company> Companies { get; set; }

        /// <summary>Sedes físicas de la organización (TB_SITE).</summary>
        public DbSet<Site> Sites { get; set; }

        /// <summary>Departamentos internos de la organización (TB_DEP).</summary>
        public DbSet<Department> Departments { get; set; }

        /// <summary>Puestos de trabajo internos (TB_ROLE).</summary>
        public DbSet<Role> Roles { get; set; }

        /// <summary>Tipos de documento de empleado (TB_DOCTYPE_EMP).</summary>
        public DbSet<DocumentTypeEmployee> DocumentTypeEmployees { get; set; }

        /// <summary>Empleados de la organización (TB_EMPLOYEE).</summary>
        public DbSet<Employee> Employees { get; set; }

        /// <summary>Roles de acceso al sistema (TB_UROLES).</summary>
        public DbSet<UserRole> UserRoles { get; set; }

        /// <summary>Cuentas de usuario del sistema (TB_USERS). Contraseñas almacenadas con hash BCrypt.</summary>
        public DbSet<User> Users { get; set; }

        // ── Catálogo de activos (jerarquía) ───────────────────────────────────

        /// <summary>Tipos generales de activo - nivel 1 (TB_GEN_ASSETS_TYPE).</summary>
        public DbSet<GenAssetType> GenAssetTypes { get; set; }

        /// <summary>Tipos específicos de activo - nivel 2 (TB_ASSETS_TYPE).</summary>
        public DbSet<AssetType> AssetTypes { get; set; }

        /// <summary>Detalles técnicos de tipos generales de activo (TB_DETAIL_GAT).</summary>
        public DbSet<DetailGat> DetailGats { get; set; }

        /// <summary>Marcas de fabricante - nivel 3 (TB_BRAND).</summary>
        public DbSet<Brand> Brands { get; set; }

        /// <summary>Modelos de equipo - nivel 4 (TB_MODEL).</summary>
        public DbSet<Model> Models { get; set; }

        // ── Hospitales ────────────────────────────────────────────────────────

        /// <summary>Unidades programáticas que agrupan hospitales (TB_PROC_UNIT).</summary>
        public DbSet<ProcessingUnit> ProcessingUnits { get; set; }

        /// <summary>Hospitales clientes (TB_HOSPITAL).</summary>
        public DbSet<Hospital> Hospitals { get; set; }

        /// <summary>Departamentos o zonas dentro de un hospital (TB_HOSP_DEP).</summary>
        public DbSet<HospitalDepartment> HospitalDepartments { get; set; }

        /// <summary>Cargos clínicos dentro de un departamento hospitalario (TB_HOSP_ROLE).</summary>
        public DbSet<HospitalRole> HospitalRoles { get; set; }

        /// <summary>Contactos clínicos (médicos, técnicos) de un hospital (TB_HOSP_DOC).</summary>
        public DbSet<HospitalDoctor> HospitalDoctors { get; set; }

        // ── TI: equipos, licitaciones, bodegas ────────────────────────────────

        /// <summary>Licitaciones o contratos de mantenimiento (TB_LIC).</summary>
        public DbSet<Licitacion> Licitaciones { get; set; }

        /// <summary>Bodegas de almacenamiento (TB_WARE). Compartidas entre TI y Servicio.</summary>
        public DbSet<Warehouse> Warehouses { get; set; }

        /// <summary>Equipos IT del inventario (TB_ITEQUIP).</summary>
        public DbSet<ItEquip> ItEquips { get; set; }

        /// <summary>Detalles técnicos adicionales de equipos IT (TB_ITEQUIP_DETAIL).</summary>
        public DbSet<ItEquipDetail> ItEquipDetails { get; set; }

        /// <summary>Historial de cambios de ubicación de equipos IT (TB_ITEQUIP_HIST).</summary>
        public DbSet<ItEquipHistory> ItEquipHistories { get; set; }

        /// <summary>Movimientos (retiros/devoluciones) de equipos IT (TB_ITEQUIP_MOV).</summary>
        public DbSet<ItEquipMovement> ItEquipMovements { get; set; }

        /// <summary>Mantenimientos programados de equipos IT (TB_ITEQUIP_MAINT).</summary>
        public DbSet<ItEquipMaintenance> ItEquipMaintenances { get; set; }

        // ── Ingeniería / Servicio ─────────────────────────────────────────────

        /// <summary>Líneas de servicio de ingeniería (TB_ENG_LINE).</summary>
        public DbSet<EngLine> EngLines { get; set; }

        /// <summary>Cajas de repuestos de ingeniería (TB_ENG_BOX).</summary>
        public DbSet<EngBox> EngBoxes { get; set; }

        /// <summary>Repuestos de ingeniería con control de stock (TB_ENG_PART).</summary>
        public DbSet<EngPart> EngParts { get; set; }

        /// <summary>Activos clínicos gestionados por ingeniería (TB_ENG_ASSET).</summary>
        public DbSet<EngAsset> EngAssets { get; set; }

        /// <summary>Historial de cambios de ubicación de activos clínicos (TB_ENG_ASSET_HIST).</summary>
        public DbSet<EngAssetHistory> EngAssetHistories { get; set; }

        /// <summary>Movimientos de stock de repuestos de ingeniería (TB_ENG_PART_MOV).</summary>
        public DbSet<EngPartMovement> EngPartMovements { get; set; }

        /// <summary>Movimientos (retiros/devoluciones) de activos clínicos (TB_ENG_ASSET_MOV).</summary>
        public DbSet<EngAssetMovement> EngAssetMovements { get; set; }

        /// <summary>Mantenimientos programados de activos clínicos (TB_ENG_MAINT).</summary>
        public DbSet<EngMaintenance> EngMaintenances { get; set; }

        /// <summary>
        /// Aplica configuraciones de modelo adicionales:
        /// clave primaria de <see cref="DetailGat"/> y longitud máxima del hash BCrypt en <see cref="User"/>.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // DetailGat uses ID as PK (no PK defined in original schema)
            modelBuilder.Entity<DetailGat>()
                .HasKey(d => d.Id);

            // USER_PASSWORD length adjustment for BCrypt (72 chars)
            modelBuilder.Entity<User>()
                .Property(u => u.UserPassword)
                .HasMaxLength(72);
        }
    }
}

namespace EDInventory.Models
{
    /// <summary>
    /// Constantes de roles del sistema y grupos de roles compuestos usados en atributos
    /// <c>[Authorize(Roles = ...)]</c> en toda la aplicación.
    /// Los grupos son cadenas separadas por coma que EF Core interpreta como OR lógico.
    /// <para>
    /// Roles individuales: <see cref="Admin"/>, <see cref="TiAdmin"/>, <see cref="TiTecnico"/>,
    /// <see cref="TiConsulta"/>, <see cref="SvcAdmin"/>, <see cref="SvcTecnico"/>, <see cref="SvcConsulta"/>.
    /// </para>
    /// <para>
    /// Grupos: <see cref="TiRead"/> / <see cref="TiWrite"/> / <see cref="TiManage"/> para módulo TI;
    /// <see cref="SvcRead"/> / <see cref="SvcWrite"/> / <see cref="SvcManage"/> para módulo Servicio;
    /// <see cref="AdmRead"/> / <see cref="AdmWrite"/> / <see cref="AdmManage"/> para recursos compartidos.
    /// </para>
    /// </summary>
    public static class AppRoles
    {
        // ── Global ────────────────────────────────────────────────────────────

        /// <summary>Administrador global del sistema. Tiene acceso total a todas las funciones.</summary>
        public const string Admin = "Administrador";

        // ── División TI ───────────────────────────────────────────────────────

        /// <summary>Administrador de la división TI. Acceso de escritura y gestión en el módulo TI.</summary>
        public const string TiAdmin    = "TI.Administrador";

        /// <summary>Técnico TI. Puede registrar y editar equipos pero no realizar acciones administrativas.</summary>
        public const string TiTecnico  = "TI.Tecnico";

        /// <summary>Consultor TI. Acceso de solo lectura al módulo TI.</summary>
        public const string TiConsulta = "TI.Consulta";

        // ── División Servicio ─────────────────────────────────────────────────

        /// <summary>Administrador de la división Servicio. Acceso de escritura y gestión en el módulo Servicio.</summary>
        public const string SvcAdmin    = "Servicio.Administrador";

        /// <summary>Técnico de Servicio. Puede registrar y editar activos y repuestos pero no acciones administrativas.</summary>
        public const string SvcTecnico  = "Servicio.Tecnico";

        /// <summary>Consultor de Servicio. Acceso de solo lectura al módulo Servicio.</summary>
        public const string SvcConsulta = "Servicio.Consulta";

        // ── Grupos módulo TI ──────────────────────────────────────────────────

        /// <summary>Grupo de solo lectura TI: todos los roles TI + SvcAdmin (visibilidad cruzada) + Admin.</summary>
        public const string TiRead   = Admin + "," + TiAdmin + "," + TiTecnico + "," + TiConsulta + "," + SvcAdmin;

        /// <summary>Grupo de escritura TI: TI.Admin + TI.Tecnico + Admin. Permite crear y editar equipos.</summary>
        public const string TiWrite  = Admin + "," + TiAdmin + "," + TiTecnico;

        /// <summary>Grupo de gestión TI: TI.Admin + Admin. Solo para acciones sensibles (borrar, importar masivo).</summary>
        public const string TiManage = Admin + "," + TiAdmin;

        // ── Grupos módulo Servicio ────────────────────────────────────────────

        /// <summary>Grupo de solo lectura Servicio: todos los roles Svc + TiAdmin (visibilidad cruzada) + Admin.</summary>
        public const string SvcRead   = Admin + "," + SvcAdmin + "," + SvcTecnico + "," + SvcConsulta + "," + TiAdmin;

        /// <summary>Grupo de escritura Servicio: Svc.Admin + Svc.Tecnico + Admin. Permite crear y editar activos.</summary>
        public const string SvcWrite  = Admin + "," + SvcAdmin + "," + SvcTecnico;

        /// <summary>Grupo de gestión Servicio: Svc.Admin + Admin. Solo para acciones sensibles.</summary>
        public const string SvcManage = Admin + "," + SvcAdmin;

        // ── Recursos compartidos (Hospitales, Licitaciones, Bodegas) ─────────

        /// <summary>Grupo de solo lectura compartido: ambas divisiones completas + Admin. Para Hospitales, Licitaciones y Bodegas.</summary>
        public const string AdmRead   = Admin + "," + TiAdmin + "," + TiTecnico + "," + TiConsulta + "," + SvcAdmin + "," + SvcTecnico + "," + SvcConsulta;

        /// <summary>Grupo de escritura compartido: admins de cualquier división + Admin global.</summary>
        public const string AdmWrite  = Admin + "," + TiAdmin + "," + SvcAdmin;

        /// <summary>Grupo de gestión compartido: admins de cualquier división + Admin global. Para acciones sensibles en recursos compartidos.</summary>
        public const string AdmManage = Admin + "," + TiAdmin + "," + SvcAdmin;
    }
}

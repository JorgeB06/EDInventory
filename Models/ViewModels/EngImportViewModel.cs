namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel de resultado para la importación masiva de repuestos de ingeniería desde Excel.
    /// Devuelto por <c>EngineeringController.ImportExcel</c> tras procesar el archivo.
    /// Detalla cuántas filas fueron procesadas, qué entidades se crearon y qué errores ocurrieron.
    /// </summary>
    public class EngImportResultViewModel
    {
        /// <summary>Total de filas de datos procesadas en el archivo Excel (sin contar la cabecera).</summary>
        public int TotalRows { get; set; }

        /// <summary>Número de líneas de servicio creadas durante la importación.</summary>
        public int LinesCreated { get; set; }

        /// <summary>Número de cajas de repuestos creadas durante la importación.</summary>
        public int BoxesCreated { get; set; }

        /// <summary>Número de repuestos insertados exitosamente.</summary>
        public int PartsInserted { get; set; }

        /// <summary>Número de repuestos omitidos por referencia duplicada o datos inválidos.</summary>
        public int PartsSkipped { get; set; }

        /// <summary>Lista de mensajes de error o advertencia encontrados durante la importación.</summary>
        public List<string> Errors { get; set; } = [];
    }
}

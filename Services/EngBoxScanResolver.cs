using EDInventory.Data;
using EDInventory.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EDInventory.Services
{
    /// <summary>
    /// Resuelve el código escaneado (pistola USB o cámara) de una caja de repuestos a su
    /// <see cref="EngBox"/>, soportando los 3 formatos usados en las etiquetas impresas:
    /// <c>ENG{BoxCode:D6}</c>, <c>ENG-{LineCode}-{BoxNumber}</c> y <c>ENG-{BoxCode}</c>.
    /// Reusado por <c>EngineeringController</c> (pistola en PC de bodega) y <c>MovilController</c> (cámara).
    /// </summary>
    public static class EngBoxScanResolver
    {
        public static async Task<EngBox?> ResolveAsync(AppDbContext context, string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var raw = code.Trim();
            EngBox? box = null;

            // Formato principal: ENG{BoxCode:D6}  ej. ENG000014
            if (raw.StartsWith("ENG", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(raw[3..], out int boxCodeA))
            {
                box = await context.EngBoxes.FirstOrDefaultAsync(b => b.BoxCode == boxCodeA);
            }

            // Fallback: ENG-{LineCode}-{BoxNumber}
            if (box == null)
            {
                var parts = raw.Split('-');
                if (parts.Length == 3 && parts[0].Equals("ENG", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parts[1], out int lineCode)
                    && int.TryParse(parts[2], out int boxNumber))
                {
                    box = await context.EngBoxes
                        .FirstOrDefaultAsync(b => b.LineCode == lineCode && b.BoxNumber == boxNumber);
                }

                // Fallback: ENG-{BoxCode}
                if (box == null && parts.Length == 2
                    && parts[0].Equals("ENG", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parts[1], out int boxCodeB))
                {
                    box = await context.EngBoxes.FirstOrDefaultAsync(b => b.BoxCode == boxCodeB);
                }
            }

            return box;
        }
    }
}

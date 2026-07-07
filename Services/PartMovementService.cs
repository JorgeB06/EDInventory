using EDInventory.Data;
using EDInventory.Models.Entities;

namespace EDInventory.Services
{
    /// <summary>
    /// Centraliza la lógica de actualizar el stock de un repuesto (<see cref="EngPart.PartQty"/>)
    /// y registrar el movimiento correspondiente (<see cref="EngPartMovement"/>). Reusado por
    /// retiros/devoluciones normales (escritorio y móvil) y por la aprobación de ajustes de
    /// auditoría de inventario, para no duplicar la lógica en cada controlador.
    /// </summary>
    public static class PartMovementService
    {
        /// <summary>
        /// Registra un retiro (<c>RETIRO</c>, resta stock) o una devolución (<c>DEVOLUCION</c>, suma stock).
        /// No valida stock suficiente: el llamador debe validarlo antes (ver <c>PartWithdraw</c>).
        /// </summary>
        public static async Task<EngPartMovement> RegisterMovementAsync(
            AppDbContext context, EngPart part, string movType, int qty, int? hospCode, string? notes, int? userCode)
        {
            int delta = movType == "RETIRO" ? -qty : qty;
            return await ApplyDeltaAsync(context, part, delta, movType, hospCode, notes, userCode);
        }

        /// <summary>
        /// Registra un ajuste de auditoría (<c>AJUSTE</c>) con un delta explícito (puede ser
        /// positivo o negativo, según la varianza aprobada). Sin hospital asociado.
        /// </summary>
        public static async Task<EngPartMovement> RegisterAdjustmentAsync(
            AppDbContext context, EngPart part, int delta, string? notes, int? userCode)
        {
            return await ApplyDeltaAsync(context, part, delta, "AJUSTE", null, notes, userCode);
        }

        private static async Task<EngPartMovement> ApplyDeltaAsync(
            AppDbContext context, EngPart part, int delta, string movType, int? hospCode, string? notes, int? userCode)
        {
            part.PartQty += delta;
            context.EngParts.Update(part);

            var movement = new EngPartMovement
            {
                PartCode     = part.PartCode,
                UserCode     = userCode,
                MovDate      = DateTime.Now,
                MovQty       = delta,
                MovType      = movType,
                HospCode     = hospCode,
                MovNotes     = notes,
                PartQtyAfter = part.PartQty
            };
            context.EngPartMovements.Add(movement);
            await context.SaveChangesAsync();
            return movement;
        }
    }
}

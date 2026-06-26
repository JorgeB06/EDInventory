using EDInventory.Models.Entities;

namespace EDInventory.Models.ViewModels
{
    public class GlobalSearchResult
    {
        public string? Query { get; set; }

        public List<ItEquip> Equipos { get; set; } = [];
        public List<EngAsset> Assets { get; set; } = [];
        public List<EngPart> Parts { get; set; } = [];
        public List<Incident> Incidents { get; set; } = [];
        public List<Hospital> Hospitals { get; set; } = [];

        public int TotalCount => Equipos.Count + Assets.Count + Parts.Count + Incidents.Count + Hospitals.Count;
    }
}

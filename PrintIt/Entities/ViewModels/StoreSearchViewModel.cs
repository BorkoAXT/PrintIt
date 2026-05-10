using Common.Enums;
using Entities.Models;

namespace Entities.ViewModels
{
    public class StoreSearchViewModel
    {
        // Filters for search
        public string? SearchString { get; set; }
        public PrintType? SelectedType { get; set; }
        public MaterialType? SelectedMaterial { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        // Results after applying filters
        public List<Print> Results { get; set; } = new();

        // Recent items for quick access
        public List<Print> RecentFigures { get; set; } = new();
        public List<Print> RecentFidgetToys { get; set; } = new();
        public List<Print> RecentAccessories { get; set; } = new();
    }
}
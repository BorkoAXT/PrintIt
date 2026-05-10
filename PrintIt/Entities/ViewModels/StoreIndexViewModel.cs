using Entities.Models;

namespace Entities.ViewModels
{
    public class StoreIndexViewModel
    {
        // Sections for recent items in each category
        public List<Print> RecentFigures { get; set; } = new List<Print>();
        public List<Print> RecentFidgetToys { get; set; } = new List<Print>();
        public List<Print> RecentAccessories { get; set; } = new List<Print>();

        // Section for search results
        public List<Print> SearchResults { get; set; } = new List<Print>();
    }
}

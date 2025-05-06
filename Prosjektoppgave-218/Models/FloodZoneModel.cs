using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Prosjektoppgave_218.Models
{
    public class FloodZoneModel
    {
        public int Id { get; set; }

        // bind the "geojson" column
        [JsonProperty("geojson")]
        public JObject GeoJsonFeature { get; set; }

        // Helper method to ensure the GeoJSON structure is valid
        public JObject GetValidGeoJsonFeature()
        {
            if (GeoJsonFeature == null)
                return null;

            // Check if the GeoJsonFeature is already a valid GeoJSON Feature
            if (GeoJsonFeature["type"] != null && GeoJsonFeature["geometry"] != null)
                return GeoJsonFeature;

            // If it's not a valid Feature, try to wrap it in a Feature structure
            return new JObject
            {
                ["type"] = "Feature",
                ["geometry"] = GeoJsonFeature,
                ["properties"] = new JObject()
            };
        }
    }
}

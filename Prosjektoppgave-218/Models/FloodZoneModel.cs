using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Prosjektoppgave_218.Models
{
    public class FloodZoneModel
    {
        // You might not even need an Id property if it's not in the response or used
        // public int Id { get; set; }

        // --- CHANGE THIS LINE ---
        // Update the JsonProperty attribute to match the key in your JSON response
        [JsonProperty("feature")] // Changed from "geojson" to "feature"
        public JObject GeoJsonFeature { get; set; }
        // --- END OF CHANGE ---

        // This method should now work correctly because GeoJsonFeature will
        // be populated with the actual GeoJSON Feature object.
        public JObject GetValidGeoJsonFeature()
        {
            if (GeoJsonFeature == null)
                return null;

            // Check if the object assigned to GeoJsonFeature is a valid Feature
            // This check should now pass correctly.
            if (GeoJsonFeature["type"] != null && GeoJsonFeature["type"].ToString() == "Feature" && GeoJsonFeature["geometry"] != null)
                return GeoJsonFeature;

            // This fallback part (wrapping a geometry) might not be necessary
            // if the "feature" key *always* contains a full Feature object,
            // but it doesn't hurt much to leave it.
            _logger.LogWarning("GeoJsonFeature received was not a standard GeoJSON Feature object, attempting to wrap."); // Optional: Add logging here if you hit this case
            return new JObject
            {
                ["type"] = "Feature",
                ["geometry"] = GeoJsonFeature, // Treat the whole object as geometry (might be wrong now)
                ["properties"] = new JObject()
            };
        }
        // Add logger if you want the warning above
        private static readonly ILogger _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<FloodZoneModel>();
    }
}
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
    }
}

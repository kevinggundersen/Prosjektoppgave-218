using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Prosjektoppgave_218.Models
{
    public class GeoJsonDataModel
    {
        public int Id { get; set; }

        [JsonProperty("geojson")]
        public JObject GeoJsonFeature { get; set; }
    }

    // If you prefer specific names:
    public class SykehusModel : GeoJsonDataModel { }
    public class PolitiFengselModel : GeoJsonDataModel { }
    public class BrannAmbulanseModel : GeoJsonDataModel { }
}
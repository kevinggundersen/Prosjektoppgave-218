using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Oppgave_2_218.Models
{
    /// <summary>
    /// Model representing power plant data with geospatial information
    /// </summary>
    public class PowerPlant
    {
        // Database columns
        public int Id { get; set; }
        public string Gml { get; set; }
        public string Objekttype { get; set; }
        public int SkaId { get; set; }
        public string SakTittel { get; set; }
        public string Tiltakshaver { get; set; }
        public int SakKategori { get; set; }
        public string Status { get; set; }
        public double Effekt_Mw { get; set; }
        public double EffektIdriftMw { get; set; }
        public double ForventetProduksjonGwh { get; set; }
        public string SakLenke { get; set; }
        public string KommuneNavn { get; set; }
        public string FylkesNavn { get; set; }
        public string IdIftDato { get; set; }
        public string UtAvDriftDato { get; set; }
        public int TotalAntTurbiner { get; set; }
        public int ObjekStatus { get; set; }
        public string LokalId { get; set; }
        public string DataUttaksDato { get; set; }
        public string EksportType { get; set; }

        // This maps directly to the coord_geojson column in your database
        [JsonProperty("coord_geojson")]
        public JObject CoordGeoJson { get; set; }

        /// <summary>
        /// Extract WGS84 coordinates (latitude, longitude) from the GeoJSON data
        /// </summary>
        public (double Lat, double Lng)? GetWGS84Coordinates()
        {
            try
            {
                // Check if we have any GeoJSON data
                if (CoordGeoJson == null)
                {
                    return null;
                }

                // Extract coordinates based on GeoJSON type
                string geoType = CoordGeoJson["type"]?.ToString();
                JToken coordinates = CoordGeoJson["coordinates"];

                if (coordinates == null)
                {
                    return null;
                }

                // Handle different GeoJSON geometry types
                if (geoType == "MultiPoint" && coordinates is JArray multiPoints && multiPoints.Count > 0)
                {
                    // For MultiPoint, take the first point
                    JArray firstPoint = multiPoints[0] as JArray;
                    if (firstPoint != null && firstPoint.Count >= 2)
                    {
                        double lng = firstPoint[0].Value<double>();
                        double lat = firstPoint[1].Value<double>();
                        return (lat, lng);
                    }
                }
                else if (geoType == "Point" && coordinates is JArray pointCoords && pointCoords.Count >= 2)
                {
                    // For Point geometry
                    double lng = pointCoords[0].Value<double>();
                    double lat = pointCoords[1].Value<double>();
                    return (lat, lng);
                }

                // No valid coordinates found
                return null;
            }
            catch (Exception)
            {
                // If anything goes wrong, return null instead of crashing
                return null;
            }
        }
    }
}
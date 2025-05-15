using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Prosjektoppgave_218.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Prosjektoppgave_218.Services
{
    public class MapService
    {
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private readonly RestClient _client;
        private string _tableName;
        private readonly ILogger<MapService> _logger;

        public MapService(IConfiguration configuration, ILogger<MapService> logger)
        {
            _supabaseUrl = configuration["Supabase:Url"];
            _supabaseKey = configuration["Supabase:ApiKey"];
            _client = new RestClient(_supabaseUrl);
            _tableName = "Vindkraftverk"; // Default table name
            _logger = logger;
        }

        /// <summary>
        /// Set the table name for this service
        /// </summary>
        public void SetTableName(string tableName)
        {
            _tableName = tableName;
        }

        /// <summary>
        /// Get all tables in the database
        /// </summary>
        public async Task<List<string>> GetTablesAsync()
        {
            try
            {
                var request = new RestRequest("/rest/v1/");
                request.Method = Method.Get;

                request.AddHeader("apikey", _supabaseKey);
                request.AddHeader("Authorization", $"Bearer {_supabaseKey}");

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    throw new Exception($"Failed to retrieve tables: {response.Content}");
                }

                var tables = JObject.Parse(response.Content);
                return tables.Properties().Select(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tables: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Get all power plants from the database
        /// </summary>
        public async Task<List<PowerPlant>> GetAllPowerPlantsAsync()
        {
            return await GetPowerPlantsAsync();
        }
        /// <summary>
        /// Get power plants with optional filtering
        /// </summary>
        public async Task<List<PowerPlant>> GetPowerPlantsAsync(string filterColumn = null, string filterOperator = null, string filterValue = null)
        {
            if (string.IsNullOrEmpty(_tableName))
            {
                throw new Exception("Table name has not been set. Call SetTableName first or check available tables with GetTablesAsync.");
            }
            var request = new RestRequest($"/rest/v1/{_tableName}");
            request.Method = Method.Get;
            // Add Supabase headers
            request.AddHeader("apikey", _supabaseKey);
            request.AddHeader("Authorization", $"Bearer {_supabaseKey}");
            request.AddHeader("Content-Type", "application/json");
            // Add basic select to get all columns including geojson
            request.AddQueryParameter("select", "*");
            // Add filtering if provided
            if (!string.IsNullOrEmpty(filterColumn) &&
                !string.IsNullOrEmpty(filterOperator) &&
                !string.IsNullOrEmpty(filterValue))
            {
                request.AddQueryParameter(filterColumn, $"{filterOperator}.{filterValue}");
            }
            RestResponse<List<PowerPlant>> response = null;
            try
            {
                response = await _client.ExecuteAsync<List<PowerPlant>>(request);
                if (!response.IsSuccessful)
                {
                    throw new Exception($"Failed to retrieve power plant data: {response.Content}");
                }
                // For debugging, output the first portion of the response
                Console.WriteLine($"Sample of response data: {response.Content.Substring(0, Math.Min(200, response.Content.Length))}");
                Console.WriteLine($"Successfully retrieved {response.Data?.Count ?? 0} power plants");
                // Check how many have valid GeoJSON data
                int plantsWithGeoJson = response.Data?.Count(p => p.CoordGeoJson != null) ?? 0;
                Console.WriteLine($"{plantsWithGeoJson} plants have CoordGeoJson data");
                return response.Data ?? new List<PowerPlant>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving power plants: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Get GeoJSON feature collection for all power plants
        /// </summary>
        public async Task<string> GetPowerPlantsGeoJsonAsync()
        {
            var powerPlants = await GetAllPowerPlantsAsync();
            Console.WriteLine($"Retrieved {powerPlants.Count} power plants from database");
            // Create GeoJSON feature collection
            var featureCollection = new
            {
                type = "FeatureCollection",
                features = powerPlants
                    .Select(p =>
                    {
                        var coords = p.GetWGS84Coordinates();
                        if (!coords.HasValue)
                        {
                            Console.WriteLine($"Plant {p.Id} ({p.SakTittel}): No valid coordinates found");
                            return null;
                        }
                        Console.WriteLine($"Plant {p.Id} ({p.SakTittel}): Using coordinates [{coords.Value.Lng}, {coords.Value.Lat}]");
                        return new
                        {
                            type = "Feature",
                            geometry = new
                            {
                                type = "Point",
                                coordinates = new double[] { coords.Value.Lng, coords.Value.Lat }
                            },
                            properties = new
                            {
                                id = p.Id,
                                name = p.SakTittel,
                                status = p.Status,
                                effect = p.Effekt_Mw,
                                municipality = p.KommuneNavn,
                                county = p.FylkesNavn,
                                turbines = p.TotalAntTurbiner
                            }
                        };
                    })
                    .Where(feature => feature != null)
                    .ToArray()
            };
            Console.WriteLine($"Created GeoJSON with {featureCollection.features.Length} features");
            return JsonConvert.SerializeObject(featureCollection);
        }
        /// <summary>
        /// Get power plants by municipality
        /// </summary>
        public async Task<List<PowerPlant>> GetPowerPlantsByMunicipalityAsync(string municipality)
        {
            return await GetPowerPlantsAsync("kommunenavn", "eq", municipality);
        }
        /// <summary>
        /// Get power plants by county
        /// </summary>
        public async Task<List<PowerPlant>> GetPowerPlantsByCountyAsync(string county)
        {
            return await GetPowerPlantsAsync("fylkesnavn", "eq", county);
        }
        /// <summary>
        /// Get power plants by status
        /// </summary>
        public async Task<List<PowerPlant>> GetPowerPlantsByStatusAsync(string status)
        {
            return await GetPowerPlantsAsync("status", "eq", status);
        }
        /// <summary>
        /// Get power plants with minimum effect
        /// </summary>
        public async Task<List<PowerPlant>> GetPowerPlantsByMinimumEffectAsync(double minEffectMw)
        {
            return await GetPowerPlantsAsync("effekt_mw", "gte", minEffectMw.ToString());
        }
        /// <summary>
        /// Get power plant by ID
        /// </summary>
        public async Task<PowerPlant> GetPowerPlantByIdAsync(int id)
        {
            var plants = await GetPowerPlantsAsync("id", "eq", id.ToString());
            return plants.FirstOrDefault();
        }
        public async Task<string> GetFloodZonesGeoJsonAsync()
        {
            // change table name
            var request = new RestRequest($"/rest/v1/Flomsoner");
            request.Method = Method.Get;
            request.AddHeader("apikey", _supabaseKey);
            request.AddHeader("Authorization", $"Bearer {_supabaseKey}");
            // select only the json column
            request.AddQueryParameter("select", "geojson");
            RestResponse<List<FloodZoneModel>> resp = null;
            try
            {
                var rawResponse = await _client.ExecuteAsync(request);
                if (!rawResponse.IsSuccessful)
                {
                    _logger.LogError($"Supabase error fetching Flood Zones: {rawResponse.StatusCode} - {rawResponse.Content}");
                    // Return an empty feature collection on error
                    return JsonConvert.SerializeObject(new { type = "FeatureCollection", features = new JObject[0] });
                }

                _logger.LogInformation($"Raw Supabase response content (Flood Zones): {rawResponse.Content}");

                var zones = JsonConvert.DeserializeObject<List<FloodZoneModel>>(rawResponse.Content) ?? new List<FloodZoneModel>();
                _logger.LogInformation($"Got {zones.Count} flood zones from database");

                // Filter out any null features and ensure valid GeoJSON structure
                var validFeatures = zones
                    .Select(z => z.GetValidGeoJsonFeature())
                    .Where(f => f != null)
                    .ToList(); // Convert to List<JObject>

                _logger.LogInformation($"Processed {validFeatures.Count} valid flood zone features");

                if (validFeatures.Count == 0)
                {
                    _logger.LogWarning("No valid flood zone features found in the response");
                    // Return an empty feature collection instead of null
                    return JsonConvert.SerializeObject(new { type = "FeatureCollection", features = new JObject[0] });
                }

                // Construct the FeatureCollection using JObject and JArray
                var featureCollection = new JObject
                {
                    ["type"] = "FeatureCollection",
                    ["features"] = new JArray(validFeatures) // Add the JObjects directly to a JArray
                };

                var result = featureCollection.ToString(Formatting.None); // Use ToString() to get the JSON string from JObject
                _logger.LogInformation($"Generated GeoJSON with {validFeatures.Count} features");
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error fetching flood zones: {e.Message}");
                // Return an empty feature collection instead of null
                return JsonConvert.SerializeObject(new { type = "FeatureCollection", features = new JObject[0] });
            }
        }
        /// <summary>
        /// Fetch only the flood zones whose geometry intersects the given bounding box (in EPSG:32633)
        /// </summary>
        public async Task<string> GetFloodZonesInBBoxAsync(
            double minx, double miny, double maxx, double maxy)
        {
            var req = new RestRequest("/rest/v1/rpc/get_flomsoner_bbox", Method.Post);
            req.AddHeader("apikey", _supabaseKey);
            req.AddHeader("Authorization", $"Bearer {_supabaseKey}");
            // send JSON keys matching your SQL function parameters:
            req.AddJsonBody(new
            {
                min_lng = minx,    // west
                min_lat = miny,    // south
                max_lng = maxx,    // east
                max_lat = maxy     // north
            });
            RestResponse<List<FloodZoneModel>> resp = null;
            try
            {
                var rawResponse = await _client.ExecuteAsync(req);
                if (!rawResponse.IsSuccessful)
                {
                    _logger.LogError($"Supabase RPC error fetching Flood Zones in BBox: {rawResponse.StatusCode} - {rawResponse.Content}");
                    // Return an empty feature collection on error
                    return JsonConvert.SerializeObject(new { type = "FeatureCollection", features = new JObject[0] });
                }
                _logger.LogInformation($"Raw Supabase response content (Flood Zones in BBox): {rawResponse.Content}");

                var zones = JsonConvert.DeserializeObject<List<FloodZoneModel>>(rawResponse.Content) ?? new List<FloodZoneModel>();
                _logger.LogInformation($"Retrieved {zones.Count} flood zones for bbox: {minx},{miny},{maxx},{maxy}");

                // Filter out any null features and ensure valid GeoJSON structure
                var validFeatures = zones
                    .Select(z => z.GetValidGeoJsonFeature())
                    .Where(f => f != null)
                    .ToList(); // Convert to List<JObject>

                _logger.LogInformation($"Processed {validFeatures.Count} valid flood zone features");

                if (validFeatures.Count == 0)
                {
                    _logger.LogWarning("No valid flood zone features found in the response");
                    // Return an empty feature collection instead of null
                    return JsonConvert.SerializeObject(new { type = "FeatureCollection", features = new JObject[0] });
                }

                // Construct the FeatureCollection using JObject and JArray
                var featureCollection = new JObject
                {
                    ["type"] = "FeatureCollection",
                    ["features"] = new JArray(validFeatures) // Add the JObjects directly to a JArray
                };

                var result = featureCollection.ToString(Formatting.None); // Use ToString() to get the JSON string from JObject
                _logger.LogInformation($"Generated GeoJSON with {validFeatures.Count} features");
                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error fetching flood zones in bounding box: {e.Message}");
                // Return an empty feature collection instead of null
                return JsonConvert.SerializeObject(new { type = "FeatureCollection", features = new JObject[0] });
            }
        }
        // In MapService.cs
        public async Task<string> GetSykehusGeoJsonAsync()
        {
            _logger.LogInformation("Fetching Sykehus GeoJSON data.");
            var request = new RestRequest($"/rest/v1/Sykehus");
            request.Method = Method.Get;
            request.AddHeader("apikey", _supabaseKey);
            request.AddHeader("Authorization", $"Bearer {_supabaseKey}");
            request.AddQueryParameter("select", "id, geojson");
            try
            {
                var response = await _client.ExecuteAsync(request); // Execute without generic type first
                _logger.LogInformation($"Raw Sykehus response: {response.Content}"); // Log the raw content
                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Supabase error fetching Sykehus: {response.StatusCode} - {response.Content}");
                    return null;
                }
                // Now deserialize the successful response
                var data = JsonConvert.DeserializeObject<List<GeoJsonDataModel>>(response.Content);
                _logger.LogInformation($"Successfully fetched {data?.Count ?? 0} Sykehus records.");

                var featureCollection = new
                {
                    type = "FeatureCollection",
                    features = data?.Select(item => item.GeoJsonFeature).ToArray() ?? new JObject[0]
                };
                var jsonResult = JsonConvert.SerializeObject(featureCollection);
                _logger.LogInformation($"Serialized Sykehus GeoJSON: {jsonResult}");
                return jsonResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing Sykehus GeoJSON.");
                return null;
            }
        }
        public async Task<string> GetPolitiFengselGeoJsonAsync()
        {
            _logger.LogInformation("Fetching Politi/Fengsel GeoJSON data.");
            var request = new RestRequest($"/rest/v1/Politi_fengsel");
            request.Method = Method.Get;
            request.AddHeader("apikey", _supabaseKey);
            request.AddHeader("Authorization", $"Bearer {_supabaseKey}");
            request.AddQueryParameter("select", "id, geojson");

            try
            {
                var response = await _client.ExecuteAsync(request); // No generic type initially

                _logger.LogInformation($"Raw Politi/Fengsel response: {response.Content}"); // Log raw content

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Supabase error fetching Politi/Fengsel: {response.StatusCode} - {response.Content}");
                    return null;
                }
                var data = JsonConvert.DeserializeObject<List<GeoJsonDataModel>>(response.Content);
                _logger.LogInformation($"Successfully fetched {data?.Count ?? 0} Politi/Fengsel records.");

                var featureCollection = new
                {
                    type = "FeatureCollection",
                    features = data?.Select(item => item.GeoJsonFeature).ToArray() ?? new JObject[0]
                };
                var jsonResult = JsonConvert.SerializeObject(featureCollection);
                _logger.LogInformation($"Serialized Politi/Fengsel GeoJSON: {jsonResult}");
                return jsonResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing Politi/Fengsel GeoJSON.");
                return null;
            }
        }
        public async Task<string> GetBrannAmbulanseGeoJsonAsync()
        {
            _logger.LogInformation("Fetching Brann/Ambulanse GeoJSON data.");
            var request = new RestRequest($"/rest/v1/Brann_ambulanse");
            request.Method = Method.Get;
            request.AddHeader("apikey", _supabaseKey);
            request.AddHeader("Authorization", $"Bearer {_supabaseKey}");
            request.AddQueryParameter("select", "id, geojson");
            try
            {
                var response = await _client.ExecuteAsync(request); // No generic type initially
                _logger.LogInformation($"Raw Politi/Fengsel response: {response.Content}"); // Log raw content

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Supabase error fetching Brann/Ambulanse: {response.StatusCode} - {response.Content}");
                    return null;
                }
                var data = JsonConvert.DeserializeObject<List<GeoJsonDataModel>>(response.Content);
                _logger.LogInformation($"Successfully fetched {data?.Count ?? 0} Brann/Ambulanse records.");

                var featureCollection = new
                {
                    type = "FeatureCollection",
                    features = data?.Select(item => item.GeoJsonFeature).ToArray() ?? new JObject[0]
                };
                var jsonResult = JsonConvert.SerializeObject(featureCollection);
                _logger.LogInformation($"Serialized Brann/Ambulanse GeoJSON: {jsonResult}");
                return jsonResult;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing Brann/Ambulanse GeoJSON.");
                return null;
            }
        }
    }
}
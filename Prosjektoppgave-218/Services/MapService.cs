using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Prosjektoppgave_218.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Prosjektoppgave_218.Services
{
    public class MapService
    {
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        private readonly RestClient _client;
        private string _tableName;

        public MapService(IConfiguration configuration)
        {
            _supabaseUrl = configuration["Supabase:Url"];
            _supabaseKey = configuration["Supabase:ApiKey"];
            _client = new RestClient(_supabaseUrl);
            _tableName = "Vindkraftverk"; // Default table name
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

            var response = await _client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to retrieve power plant data: {response.Content}");
            }

            // For debugging, output the first portion of the response
            Console.WriteLine($"Sample of response data: {response.Content.Substring(0, Math.Min(200, response.Content.Length))}");

            try
            {
                var powerPlants = JsonConvert.DeserializeObject<List<PowerPlant>>(response.Content);
                Console.WriteLine($"Successfully deserialized {powerPlants.Count} power plants");

                // Check how many have valid GeoJSON data
                int plantsWithGeoJson = powerPlants.Count(p => p.CoordGeoJson != null);
                Console.WriteLine($"{plantsWithGeoJson} plants have CoordGeoJson data");

                return powerPlants;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing power plants: {ex.Message}");
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
            var resp = await _client.ExecuteAsync(request);
            var zones = JsonConvert.DeserializeObject<List<FloodZoneModel>>(resp.Content);
            Console.WriteLine($"Got {zones.Count} zones; sample 1: {zones.FirstOrDefault()?.GeoJsonFeature}");

            var fc = new
            {
                type = "FeatureCollection",
                features = zones
                .Select(z => z.GeoJsonFeature)   // each is already a GeoJSON Feature
                .ToArray()
            };
            return JsonConvert.SerializeObject(fc);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Prosjektoppgave_218.Models;
using Prosjektoppgave_218.Services;
using System;
using System.Threading.Tasks;

namespace Prosjektoppgave_218.Controllers
{
    public class MapController : Controller
    {
        private readonly MapService _powerPlantService;
        private readonly ILogger<MapController> _logger;

        public MapController(MapService powerPlantService, ILogger<MapController> logger)
        {
            _powerPlantService = powerPlantService;
            _logger = logger;
        }

        // GET: PowerPlant
        public async Task<IActionResult> Index()
        {
            try
            {
                var powerPlants = await _powerPlantService.GetAllPowerPlantsAsync();
                return View(powerPlants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving power plants");
                return View("Map");
            }
        }

        // GET: PowerPlant/Map
        public IActionResult Map()
        {
            return View();
        }

        // GET: PowerPlant/GeoJson
        [HttpGet]
        public async Task<IActionResult> GeoJson()
        {
            try
            {
                var geoJson = await _powerPlantService.GetPowerPlantsGeoJsonAsync();
                _logger.LogInformation($"GeoJSON data: {geoJson}");
                return Content(geoJson, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving GeoJSON data");
                return StatusCode(500, "Failed to retrieve geographic data");
            }
        }

        // GET: PowerPlant/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var powerPlant = await _powerPlantService.GetPowerPlantByIdAsync(id);
                if (powerPlant == null)
                {
                    return NotFound();
                }
                return View(powerPlant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving power plant with ID {id}");
                return View("Map");
            }
        }

        // GET: PowerPlant/ByMunicipality/{municipality}
        public async Task<IActionResult> ByMunicipality(string municipality)
        {
            try
            {
                var powerPlants = await _powerPlantService.GetPowerPlantsByMunicipalityAsync(municipality);
                ViewBag.Municipality = municipality;
                return View("Index", powerPlants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving power plants for municipality {municipality}");
                return View("Map");
            }
        }

        // GET: PowerPlant/ByCounty/{county}
        public async Task<IActionResult> ByCounty(string county)
        {
            try
            {
                var powerPlants = await _powerPlantService.GetPowerPlantsByCountyAsync(county);
                ViewBag.County = county;
                return View("Index", powerPlants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving power plants for county {county}");
                return View("Map");
            }
        }

        // GET: PowerPlant/ByStatus/{status}
        public async Task<IActionResult> ByStatus(string status)
        {
            try
            {
                var powerPlants = await _powerPlantService.GetPowerPlantsByStatusAsync(status);
                ViewBag.Status = status;
                return View("Index", powerPlants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving power plants with status {status}");
                return View("Map");
            }
        }

        // GET: PowerPlant/ByMinimumEffect?minEffect=10.5
        public async Task<IActionResult> ByMinimumEffect(double minEffect)
        {
            try
            {
                var powerPlants = await _powerPlantService.GetPowerPlantsByMinimumEffectAsync(minEffect);
                ViewBag.MinimumEffect = minEffect;
                return View("Index", powerPlants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving power plants with minimum effect {minEffect}");
                return View("Map");
            }
        }
    }
}

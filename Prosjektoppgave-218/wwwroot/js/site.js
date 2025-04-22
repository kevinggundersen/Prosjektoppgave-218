// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var map = L.map('map').setView([58.1633, 8.0025], 8);

//Kartverket
var kartverket = L.tileLayer('https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png', {
    maxZoom: 19,
    attribution: '&copy; Kartverket</a>'
}).addTo(map);

// Tilelayer for wind resources
var windSource =L.tileLayer.wms("https://nve.geodataonline.no/arcgis/services/Vindressurser/MapServer/WMSServer", {
    // Replace '0' with the actual layer name as defined in the GetCapabilities document
    layers: "Gj.snittlig_vindstyrke_120m_over_bakkeniva",
    format: "image/png",
    transparent: true,
    attribution: "NVE Geodata Online",
    opacity: 1
}).addTo(map);

// Add the WMS layer for terrain complexity
var terrainComp = L.tileLayer.wms("https://nve.geodataonline.no/arcgis/services/Vindressurser/MapServer/WMSServer", {
    // Replace '0' with the actual layer name as defined in the GetCapabilities document
    layers: "Terrengkompleksitet_RIX",
    format: "image/png",
    transparent: true,
    attribution: "NVE Geodata Online",
    opacity: 1,
    className: 'blend-layer'
}).addTo(map);

//Google labels
var googleLabels = L.tileLayer('https://mt1.google.com/vt/lyrs=h&x={x}&y={y}&z={z}', {
    maxZoom: 19,
    attribution: '&copy; Google</a>'
}).addTo(map);

// Layer control
var baseMaps = {
    "kartverket": kartverket
};

map.addLayer(windSource);
map.addLayer(terrainComp);
map.addLayer(googleLabels);

var overlayMaps = {
    "wind Source": windSource,
    "terrain Complexity": terrainComp,
    "google Labels": googleLabels
};

var layerControl = L.control.layers(baseMaps, overlayMaps).addTo(map);

map.removeLayer(terrainComp);
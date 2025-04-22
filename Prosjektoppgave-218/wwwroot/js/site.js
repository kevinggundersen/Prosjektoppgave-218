// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var map = L.map('map').setView([58.1633, 8.0025], 8);

//Kartverket
var kartverket = L.tileLayer('https://cache.kartverket.no/v1/wmts/1.0.0/topo/default/webmercator/{z}/{y}/{x}.png', {
    maxZoom: 19,
    attribution: '&copy; Kartverket</a>'
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

map.addLayer(googleLabels);

var overlayMaps = {
    "google Labels": googleLabels
};

var layerControl = L.control.layers(baseMaps, overlayMaps).addTo(map);

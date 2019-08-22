
using OsmSharp;
using OsmSharp.Streams;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Transidious
{

public class OSMImportHelper {
    public enum Area {
Default,
Berlin,
Charlottenburg,
CharlottenburgWilmersdorf,
Mitte,
Spandau,
Saarbruecken,
Andorra,
Karlsruhe,
Freiburg,
Frankfurt,
London,
NewYorkCity,
Werder,
Seattle,
Paris,

    }

    OSMImporter importer;
    Area area = Area.Default;

    Stream input = null;
    PBFOsmStreamSource sourceStream = null;

    public OSMImportHelper(OSMImporter importer, string area, string country)
    {
         this.importer = importer;

         string fileName;
         fileName = "Resources/OSM/";
         fileName += country;
         fileName += "/";
         fileName += area;
         fileName += ".osm.pbf";

        input = File.OpenRead(fileName);
        if (input == null)
        {
            Debug.LogError("opening stream failed");
            return;
        }

        this.sourceStream = new PBFOsmStreamSource(input);
        Enum.TryParse(area, true, out this.area);
        
        PBFOsmStreamSource allNodes = null;
switch (this.area) {
    case Area.Default: {
allNodes = this.sourceStream;
        }
        break;    case Area.Berlin: {
allNodes = this.sourceStream;
        }
        break;    case Area.Charlottenburg: {

               string allNodesFileName;
               allNodesFileName = "Resources/OSM/";
               allNodesFileName += "Deutschland/CharlottenburgWilmersdorf";
               allNodesFileName += ".osm.pbf";

               var allNodesInput = File.OpenRead(allNodesFileName);
               if (allNodesInput == null)
               {
                     Debug.LogError("opening stream failed");
                     return;
               }

               allNodes = new PBFOsmStreamSource(allNodesInput);
        }
        break;    case Area.CharlottenburgWilmersdorf: {

               string allNodesFileName;
               allNodesFileName = "Resources/OSM/";
               allNodesFileName += "Deutschland/Berlin";
               allNodesFileName += ".osm.pbf";

               var allNodesInput = File.OpenRead(allNodesFileName);
               if (allNodesInput == null)
               {
                     Debug.LogError("opening stream failed");
                     return;
               }

               allNodes = new PBFOsmStreamSource(allNodesInput);
        }
        break;    case Area.Mitte: {

               string allNodesFileName;
               allNodesFileName = "Resources/OSM/";
               allNodesFileName += "Deutschland/Berlin";
               allNodesFileName += ".osm.pbf";

               var allNodesInput = File.OpenRead(allNodesFileName);
               if (allNodesInput == null)
               {
                     Debug.LogError("opening stream failed");
                     return;
               }

               allNodes = new PBFOsmStreamSource(allNodesInput);
        }
        break;    case Area.Spandau: {

               string allNodesFileName;
               allNodesFileName = "Resources/OSM/";
               allNodesFileName += "Deutschland/Berlin";
               allNodesFileName += ".osm.pbf";

               var allNodesInput = File.OpenRead(allNodesFileName);
               if (allNodesInput == null)
               {
                     Debug.LogError("opening stream failed");
                     return;
               }

               allNodes = new PBFOsmStreamSource(allNodesInput);
        }
        break;    case Area.Saarbruecken: {
allNodes = this.sourceStream;
        }
        break;    case Area.Andorra: {
allNodes = this.sourceStream;
        }
        break;    case Area.Karlsruhe: {
allNodes = this.sourceStream;
        }
        break;    case Area.Freiburg: {
allNodes = this.sourceStream;
        }
        break;    case Area.Frankfurt: {
allNodes = this.sourceStream;
        }
        break;    case Area.London: {
allNodes = this.sourceStream;
        }
        break;    case Area.NewYorkCity: {
allNodes = this.sourceStream;
        }
        break;    case Area.Werder: {
allNodes = this.sourceStream;
        }
        break;    case Area.Seattle: {
allNodes = this.sourceStream;
        }
        break;    case Area.Paris: {
allNodes = this.sourceStream;
        }
        break;}

         foreach (var geo in allNodes)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                  continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  break;
               case OsmGeoType.Way:
                  if (geo.Id.HasValue) importer.ways.Add(geo.Id.Value, geo as Way);
                  break;
               case OsmGeoType.Node:
                  if (geo.Id.HasValue) importer.nodes.Add(geo.Id.Value, geo as Node);
                  break;
            }
         }
    }

    public void ImportArea()
    {
switch (this.area) {
    case Area.Default: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {

break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Berlin: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Berlin")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "4")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Charlottenburg: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Charlottenburg")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "10")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.CharlottenburgWilmersdorf: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Charlottenburg-Wilmersdorf")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "9")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Mitte: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Mitte")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "9")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Spandau: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Spandau")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "9")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Saarbruecken: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Saarbrücken")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Andorra: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Andorra")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "2")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Karlsruhe: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Karlsruhe")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "6")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Freiburg: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Freiburg")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "6")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Frankfurt: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Frankfurt")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "6")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.London: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "London")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "6")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.NewYorkCity: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "NewYorkCity")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "5")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Werder: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Werder")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "9")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Seattle: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Seattle")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Paris: {

         foreach (var geo in sourceStream)
         {
            var tags = geo.Tags;
            if (tags == null)
            {
                continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  var rel = geo as Relation;
                  
while (true) {
if (tags.Contains("name", "Paris")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; break;

}}
break;
}

while (true) {

         TransitType type;
if (tags.Contains("type", "route") && tags.Contains("route", "bus")) {
type = TransitType.Bus;
} else if (tags.Contains("type", "route") && tags.Contains("route", "tram")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "light_rail")) {
type = TransitType.Tram;
} else if (tags.Contains("type", "route") && tags.Contains("route", "subway")) {
type = TransitType.Subway;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("line", "light_rail")) {
type = TransitType.LightRail;
} else if (tags.Contains("type", "route") && tags.Contains("route", "train") && tags.Contains("service", "regional")) {
type = TransitType.LightRail;
}
                     else
                     {
                        break;
                     }

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporter.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporter.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential));
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary));
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary));
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary));
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link));
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River));
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
}if (tags.Contains("natural", "water") && tags.Contains("water", "lake")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("natural", "water") && tags.Contains("water", "pond")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
}
break;
}

               break;
               case OsmGeoType.Node:
                  var node = geo as Node;
                  {
                     double lat = node.Latitude.Value;
                     double lng = node.Longitude.Value;

                     importer.minLat = System.Math.Min(lat, importer.minLat);
                     importer.minLng = System.Math.Min(lng, importer.minLng);

                     importer.maxLat = System.Math.Max(lat, importer.maxLat);
                     importer.maxLng = System.Math.Max(lng, importer.maxLng);
                  }

while (true) {
if (tags.Contains("highway", "bus_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node);
}
break;
}

               break;
            }
         }
        }
        break;}

   }
}
}

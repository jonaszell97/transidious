
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
Seattle,
Paris,
NiagaraFalls,
Konstanz,
München,
Paradise,
Seaside,
Salinas,
Werder,
Manhattan,
Vancouver,

    }

    OSMImporterProxy importer;
    Area area = Area.Default;

    Stream input = null;
    PBFOsmStreamSource sourceStream = null;

    HashSet<long> referencedGeos;

    void AddGeoReference(OsmGeo geo)
    {
       if (geo.Type == OsmGeoType.Node)
       {
          if (geo.Id.HasValue)
            referencedGeos.Add(geo.Id.Value);
       }
       else if (geo.Type == OsmGeoType.Way)
       {
          if (geo.Id.HasValue)
            referencedGeos.Add(geo.Id.Value);
       }
       else
       {
          var rel = geo as Relation;
          foreach (var member in rel.Members)
          {
             referencedGeos.Add(member.Id);
          }
       }
    }

    public OSMImportHelper(OSMImporterProxy importer, string area, string country)
    {
         this.importer = importer;
         this.referencedGeos = new HashSet<long>();

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
allNodes = this.sourceStream;
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
        break;    case Area.Seattle: {
allNodes = this.sourceStream;
        }
        break;    case Area.Paris: {
allNodes = this.sourceStream;
        }
        break;    case Area.NiagaraFalls: {
allNodes = this.sourceStream;
        }
        break;    case Area.Konstanz: {
allNodes = this.sourceStream;
        }
        break;    case Area.München: {
allNodes = this.sourceStream;
        }
        break;    case Area.Paradise: {
allNodes = this.sourceStream;
        }
        break;    case Area.Seaside: {
allNodes = this.sourceStream;
        }
        break;    case Area.Salinas: {
allNodes = this.sourceStream;
        }
        break;    case Area.Werder: {
allNodes = this.sourceStream;
        }
        break;    case Area.Manhattan: {
allNodes = this.sourceStream;
        }
        break;    case Area.Vancouver: {
allNodes = this.sourceStream;
        }
        break;}


       this.ImportArea(allNodes);
    }

    void ImportArea(PBFOsmStreamSource allNodes)
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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
if (tags.Contains("name", "Freiburg im Breisgau")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "6")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
if (tags.Contains("name", "Frankfurt am Main")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "6")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.NiagaraFalls: {

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
if (tags.Contains("name", "NiagaraFalls")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Konstanz: {

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
if (tags.Contains("name", "Konstanz")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.München: {

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
if (tags.Contains("name", "München")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "6")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Paradise: {

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
if (tags.Contains("name", "Paradise")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Seaside: {

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
if (tags.Contains("name", "Seaside")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Salinas: {

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
if (tags.Contains("name", "Salinas")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
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
if (tags.Contains("name", "Werder (Havel)")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Manhattan: {

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
if (tags.Contains("name", "Manhattan")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "7")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;    case Area.Vancouver: {

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
if (tags.Contains("name", "Vancouver")) {if (tags.Contains("type", "boundary") && tags.Contains("admin_level", "8")) {
                importer.boundary = geo as Relation; AddGeoReference(geo); break;

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

                     AddGeoReference(rel);

                     var lineName = tags.GetValue("ref");
                     if (importer.lines.TryGetValue(lineName, out OSMImporterProxy.TransitLine pair))
                     {
                         pair.outbound = rel;
                     }
                     else
                     {
                         importer.lines.Add(lineName, new OSMImporterProxy.TransitLine {
                             inbound = rel,
                             type = type
                         });
                     }

break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Park));
AddGeoReference(rel);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Green));
AddGeoReference(rel);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Allotment));
AddGeoReference(rel);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Cemetery));
AddGeoReference(rel);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.SportsPitch));
AddGeoReference(rel);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Lake));
AddGeoReference(rel);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.FootpathArea));
AddGeoReference(rel);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Forest));
AddGeoReference(rel);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Beach));
AddGeoReference(rel);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type.Parking));
AddGeoReference(rel);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Residential));
AddGeoReference(rel);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Shop));
AddGeoReference(rel);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Office));
AddGeoReference(rel);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.University));
AddGeoReference(rel);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Hospital));
AddGeoReference(rel);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.HighSchool));
AddGeoReference(rel);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type.Stadium));
AddGeoReference(rel);
}
break;
}

               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
while (true) {
if (tags.Contains("highway", "motorway")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Highway)); AddGeoReference(way);
} else if (tags.Contains("highway", "residential")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "living_street")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Residential)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Primary)); AddGeoReference(way);
} else if (tags.Contains("highway", "motorway_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "primary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Secondary)); AddGeoReference(way);
} else if (tags.Contains("highway", "secondary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Tertiary)); AddGeoReference(way);
} else if (tags.Contains("highway", "tertiary_link")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.Link)); AddGeoReference(way);
} else if (tags.Contains("waterway", "river")) {
importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type.River)); AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("leisure", "park")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Park));
AddGeoReference(way);
}if (tags.Contains("landuse", "village_green")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "grass")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("leisure", "sports_centre")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "recreation_ground")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Green));
AddGeoReference(way);
}if (tags.Contains("landuse", "allotments")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Allotment));
AddGeoReference(way);
}if (tags.Contains("landuse", "cemetery")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Cemetery));
AddGeoReference(way);
}if (tags.Contains("leisure", "pitch")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("leisure", "track")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.SportsPitch));
AddGeoReference(way);
}if (tags.Contains("natural", "water")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Lake));
AddGeoReference(way);
}if (tags.Contains("highway", "footpath") && tags.Contains("area", "yes")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.FootpathArea));
AddGeoReference(way);
}if (tags.Contains("landuse", "forest")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Forest));
AddGeoReference(way);
}if (tags.Contains("natural", "beach")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Beach));
AddGeoReference(way);
}if (tags.Contains("amenity", "parking")) {
importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type.Parking));
AddGeoReference(way);
}
break;
}

while (true) {
if (tags.Contains("building", "residential")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "house")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "yes")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Residential));
AddGeoReference(way);
}if (tags.Contains("building", "retail")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "commercial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Shop));
AddGeoReference(way);
}if (tags.Contains("building", "industrial")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "office")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Office));
AddGeoReference(way);
}if (tags.Contains("building", "university")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.University));
AddGeoReference(way);
}if (tags.Contains("building", "hospital")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Hospital));
AddGeoReference(way);
}if (tags.Contains("building", "school")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.HighSchool));
AddGeoReference(way);
}if (tags.Contains("building", "stadium")) {
importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type.Stadium));
AddGeoReference(way);
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
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("public_transport", "stop_position")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
} else if (tags.Contains("railway", "tram_stop_exit_only")) {
Debug.Assert(geo.Id.HasValue, "stop does not have an ID");importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);
}
break;
}

               break;
            }
         }
        }
        break;}

      OsmGeo[] nodes = null;
      GameController.instance.RunTimer("ToArray", () =>
      {
         nodes = allNodes.ToArray();
      });

      foreach (var way in nodes.OfType<Way>())
         {
            if (!way.Id.HasValue || !referencedGeos.Contains(way.Id.Value))
            {
                  continue;
            }

            foreach (var nodeId in way.Nodes)
            {
               referencedGeos.Add(nodeId);
            }
         }

         foreach (var geo in nodes)
         {
            if (!geo.Id.HasValue || !referencedGeos.Contains(geo.Id.Value))
            {
                  continue;
            }

            switch (geo.Type)
            {
               case OsmGeoType.Relation:
                  break;
               case OsmGeoType.Way:
                  importer.ways.Add(geo.Id.Value, geo as Way);
                  break;
               case OsmGeoType.Node:
                  importer.nodes.Add(geo.Id.Value, geo as Node);
                  break;
            }
         }
   }
}
}

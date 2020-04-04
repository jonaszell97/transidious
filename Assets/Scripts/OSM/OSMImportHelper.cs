

#if UNITY_EDITOR

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

public class OSMImportHelper
{
    public enum Area
    {
        Default,
        Berlin,
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
        Fuerteventura,
        Charlottenburg,
        CharlottenburgWilmersdorf,
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

    public OSMImportHelper(OSMImporterProxy importer, string area, string file)
    {
        this.importer = importer;
        this.referencedGeos = new HashSet<long>();
        Enum.TryParse(area, true, out this.area);

        PBFOsmStreamSource allNodes;
        switch (this.area)
        {
        default:
        case Area.Default :
        {
            allNodes = null;

            break;
        }
        case Area.Berlin :
        {
            allNodes = null;

            break;
        }
        case Area.Mitte :
        {
            string allNodesFileName = "Resources/OSM/";
            allNodesFileName += "Deutschland/Berlin";
            allNodesFileName += ".osm.pbf";
            var allNodesInput = File.OpenRead(allNodesFileName);
            if (allNodesInput == null)
            {
                Debug.LogError("opening stream failed");
                return;
            }

            allNodes = new PBFOsmStreamSource(allNodesInput);

            break;
        }
        case Area.Spandau :
        {
            string allNodesFileName = "Resources/OSM/";
            allNodesFileName += "Deutschland/Berlin";
            allNodesFileName += ".osm.pbf";
            var allNodesInput = File.OpenRead(allNodesFileName);
            if (allNodesInput == null)
            {
                Debug.LogError("opening stream failed");
                return;
            }

            allNodes = new PBFOsmStreamSource(allNodesInput);

            break;
        }
        case Area.Saarbruecken :
        {
            allNodes = null;

            break;
        }
        case Area.Andorra :
        {
            allNodes = null;

            break;
        }
        case Area.Karlsruhe :
        {
            allNodes = null;

            break;
        }
        case Area.Freiburg :
        {
            allNodes = null;

            break;
        }
        case Area.Frankfurt :
        {
            allNodes = null;

            break;
        }
        case Area.London :
        {
            allNodes = null;

            break;
        }
        case Area.NewYorkCity :
        {
            allNodes = null;

            break;
        }
        case Area.Seattle :
        {
            allNodes = null;

            break;
        }
        case Area.Paris :
        {
            allNodes = null;

            break;
        }
        case Area.NiagaraFalls :
        {
            allNodes = null;

            break;
        }
        case Area.Konstanz :
        {
            allNodes = null;

            break;
        }
        case Area.München :
        {
            allNodes = null;

            break;
        }
        case Area.Paradise :
        {
            allNodes = null;

            break;
        }
        case Area.Seaside :
        {
            allNodes = null;

            break;
        }
        case Area.Salinas :
        {
            allNodes = null;

            break;
        }
        case Area.Werder :
        {
            allNodes = null;

            break;
        }
        case Area.Manhattan :
        {
            allNodes = null;

            break;
        }
        case Area.Vancouver :
        {
            allNodes = null;

            break;
        }
        case Area.Fuerteventura :
        {
            allNodes = null;

            break;
        }
        case Area.Charlottenburg :
        {
            allNodes = null;

            break;
        }
        case Area.CharlottenburgWilmersdorf :
        {
            allNodes = null;

            break;
        }
        }

        string fileName;
        fileName = "Resources/OSM/";
        fileName += file;
        fileName += ".osm.pbf";

        if (!File.Exists(fileName))
        {
            Debug.Assert(allNodes != null, "no source file!");

            using (this.CreateTimer("Create Area"))
            {
                CreateAreaFile(allNodes, file);
            }
        }

        this.input = File.OpenRead(fileName);
        this.sourceStream = new PBFOsmStreamSource(input);

        if (allNodes == null)
        {
            allNodes = this.sourceStream;
        }

        this.ImportArea(allNodes);
    }

    bool CheckGeo(OsmGeo geo, Rect rect, Dictionary<long, OsmGeo> geos)
    {
        switch (geo.Type)
        {
            default:
            case OsmGeoType.Node:
                return CheckNode(geo as Node, rect, geos);
            case OsmGeoType.Way:
                return CheckWay(geo as Way, rect, geos);
            case OsmGeoType.Relation:
                return CheckRelation(geo as Relation, rect, geos);
        }
    }

    bool CheckNode(Node node, Rect rect, Dictionary<long, OsmGeo> geos)
    {
        var pos = new Vector2((float)node.Longitude.Value, (float)node.Latitude.Value);
        if (rect.Contains(pos))
        {
            return true;
        }

        return false;
    }

    bool CheckWay(Way way, Rect rect, Dictionary<long, OsmGeo> geos)
    {
        foreach (var nodeId in way.Nodes)
        {
            if (!geos.TryGetValue(nodeId, out OsmGeo node))
            {
                continue;
            }

            if (CheckGeo(node, rect, geos))
            {
                return true;
            }
        }

        return false;
    }

    bool CheckRelation(Relation rel, Rect rect, Dictionary<long, OsmGeo> geos)
    {
        foreach (var member in rel.Members)
        {
            if (!geos.TryGetValue(member.Id, out OsmGeo node))
            {
                continue;
            }

            if (CheckGeo(node, rect, geos))
            {
                return true;
            }
        }

        return false;
    }

    void AddGeo(OsmGeo geo, Dictionary<long, OsmGeo> geos, HashSet<OsmGeo> target)
    {
        switch (geo.Type)
        {
            default:
            case OsmGeoType.Node:
                AddNode(geo as Node, geos, target);
                break;
            case OsmGeoType.Way:
                AddWay(geo as Way, geos, target);
                break;
            case OsmGeoType.Relation:
                AddRelation(geo as Relation, geos, target);
                break;
        }
    }

    void AddNode(Node node, Dictionary<long, OsmGeo> geos, HashSet<OsmGeo> target)
    {
        target.Add(node);
    }

    void AddWay(Way way, Dictionary<long, OsmGeo> geos, HashSet<OsmGeo> target)
    {
        foreach (var nodeId in way.Nodes)
        {
            if (!geos.TryGetValue(nodeId, out OsmGeo node))
            {
                continue;
            }

            AddGeo(node, geos, target);
        }

        target.Add(way);
    }

    void AddRelation(Relation rel, Dictionary<long, OsmGeo> geos, HashSet<OsmGeo> target)
    {
        foreach (var member in rel.Members)
        {
            if (!geos.TryGetValue(member.Id, out OsmGeo node))
            {
                continue;
            }

            AddGeo(node, geos, target);
        }

        target.Add(rel);
    }

    void CreateAreaFile(PBFOsmStreamSource allNodes, string file)
    {
        var polyfileName = $"Resources/Poly/{file}.poly";
        var lines = File.ReadAllLines(polyfileName);

        var boundaryCoords = new List<Vector2>();
        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;

        for (var i = 2; i < lines.Length - 2; ++i)
        {
            var line = lines[i];
            var xy = line.Trim().Split(' ');

            if (!float.TryParse(xy[0], out float lng))
            {
                Debug.LogError($"invalid float {xy[0]}");
                continue;
            }
            if (!float.TryParse(xy[2], out float lat))
            {
                Debug.LogError($"invalid float {xy[2]}");
                continue;
            }

            minX = Mathf.Min(minX, lng);
            minY = Mathf.Min(minY, lat);
            maxX = Mathf.Max(maxX, lng);
            maxY = Mathf.Max(maxY, lat);

            boundaryCoords.Add(new Vector2(lng, lat));
        }
        
        var outfileName = $"Resources/OSM/{file}.osm.pbf";
        using (var output = File.OpenWrite(outfileName))
        {
            var target = new PBFOsmStreamTarget(output);
            target.Initialize();

            var rect = new Rect(minX, minY, maxX - minX, maxY - minY);

            var geos = new Dictionary<long, OsmGeo>();
            using (this.CreateTimer("ToDict"))
            {
                foreach (var geo in allNodes.ToArray())
                {
                    if (geos.ContainsKey(geo.Id.Value))
                        continue;

                    geos.Add(geo.Id.Value, geo);
                }
            }

            var referenced = new HashSet<OsmGeo>();
            foreach (var geo in geos)
            {
                if (CheckGeo(geo.Value, rect, geos))
                {
                    AddGeo(geo.Value, geos, referenced);
                }
            }

            foreach (var geo in referenced)
            {
                switch (geo.Type)
                {
                    case OsmGeoType.Node:
                        target.AddNode(geo as Node);
                        break;
                    case OsmGeoType.Way:
                        target.AddWay(geo as Way);
                        break;
                    case OsmGeoType.Relation:
                        target.AddRelation(geo as Relation);
                        break;
                }
            }

            target.Flush();
            target.Close();
        }
    }

    void ImportArea(PBFOsmStreamSource allNodes)
    {
        switch (this.area)
        {
        case Area.Default :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Berlin :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Berlin";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Mitte :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Mitte";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Spandau :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Spandau";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Saarbruecken :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Saarbrücken";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Andorra :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Andorra";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Karlsruhe :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Karlsruhe";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Freiburg :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Freiburg im Breisgau";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Frankfurt :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Frankfurt am Main";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.London :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "London";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.NewYorkCity :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "NewYorkCity";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Seattle :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Seattle";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Paris :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Paris";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.NiagaraFalls :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "NiagaraFalls";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Konstanz :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Konstanz";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.München :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "München";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Paradise :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Paradise";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Seaside :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Seaside";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Salinas :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Salinas";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Werder :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Werder (Havel)";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Manhattan :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Manhattan";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Vancouver :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Vancouver";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Fuerteventura :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Fuerteventura";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.Charlottenburg :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Charlottenburg";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        case Area.CharlottenburgWilmersdorf :
        {
            foreach (var geo in sourceStream)
            {
                var tags = geo.Tags;
                if (tags == null)
                {
                    continue;
                }

                // Check boundary.
                if (geo.Type == OsmGeoType.Relation)
                {
                    string boundaryName = "Charlottenburg-Wilmersdorf";
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("leisure", "park")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Park));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "village_green")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "grass")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "recreation_ground")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "heath")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "grassland")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Green));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "zoo")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Zoo));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "allotments")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Allotment));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "cemetery")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Cemetery));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "pitch")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "track")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.SportsPitch));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "water")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("leisure", "swimming_pool")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Lake));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "footpath") && 
        tags.Contains("area", "yes")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.FootpathArea));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "forest")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Forest));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "beach")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("natural", "sand")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Beach));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("amenity", "parking")
    )

                    {
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Parking));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "residential")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("landuse", "railway")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.Railway));
                        AddGeoReference(geo);
                    }
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
    if (
        tags.Contains("building", "residential")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "house")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "yes")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "apartments")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "garage")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "construction")
    )

                    {
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail") && 
        tags.Contains("shop", "supermarket")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.GroceryStore));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "retail")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "commercial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "mall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Shop));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public") && 
        tags.Contains("leisure", "sports_centre")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "concert_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "exhibition_hall")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Leisure));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "industrial")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "office")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "public")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "government")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "embassy")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Office));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "manufacture")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "warehouse")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Industrial));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "university")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.University));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "school")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.HighSchool));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hospital")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hospital));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "stadium")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Stadium));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "airport")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Airport));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "church")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Church));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "castle")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Sight));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("building", "hotel")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Hotel));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.ContainsKey("building")
    )

                    {
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.Other));
                        AddGeoReference(geo);
                    }
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "bus")
    )

                        {
                            type = TransitType.Bus;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "tram")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "light_rail")
    )

                        {
                            type = TransitType.Tram;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "subway")
    )

                        {
                            type = TransitType.Subway;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("line", "light_rail")
    )

                        {
                            type = TransitType.LightRail;
                        } else 
    if (
        tags.Contains("type", "route") && 
        tags.Contains("route", "train") && 
        tags.Contains("service", "regional")
    )

                        {
                            type = TransitType.LightRail;
                        }
                        else
                        {
                            break;
                        }

                        var rel = geo as Relation;
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
                }

                // Check transit stops.
                if (geo.Type == OsmGeoType.Node)
                {
    if (
        tags.Contains("highway", "bus_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("public_transport", "stop_position")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("railway", "tram_stop_exit_only")
    )

                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
    if (
        tags.Contains("highway", "motorway")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Highway));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "residential")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "living_street")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Residential));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Primary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "motorway_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "primary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Secondary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "secondary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "unclassified")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Tertiary));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("highway", "tertiary_link")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.Link));
                        AddGeoReference(geo);
                    } else 
    if (
        tags.Contains("waterway", "river")
    )

                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.River));
                        AddGeoReference(geo);
                    }
                }

                // Update max and min.
                if (geo.Type == OsmGeoType.Node)
                {
                    var node = geo as Node;
                    double lat = node.Latitude.Value;
                    double lng = node.Longitude.Value;

                    importer.minLat = System.Math.Min(lat, importer.minLat);
                    importer.minLng = System.Math.Min(lng, importer.minLng);

                    importer.maxLat = System.Math.Max(lat, importer.maxLat);
                    importer.maxLng = System.Math.Max(lng, importer.maxLng);
                }
            }

            break;
        }
        }

        OsmGeo[] nodes = null;
        using (this.CreateTimer("ToArray"))
        {
            nodes = allNodes.ToArray();
        }

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

#endif
<% define if_tags_match(tags) %>
    if (
    <% for_each | $(tags) as TAG, i %>
        <% if | !gt($(i), 0) %> && <% end %>
        <% if | !empty($(TAG).value) %>
        tags.ContainsKey(<%% str | $(TAG).key %%>)
        <% else %>
        tags.Contains(<%% str | $(TAG).key %%>, <%% str | $(TAG).value %%>)
        <% end %>
    <% end %>
    )
<% end %>

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
    <% for_each_record | "Area" as AREA %>
        <%% record_name | $(AREA) %%>,
    <% end %>
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

         string fileName;
         fileName = "Resources/OSM/";
         fileName += file;
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
        switch (this.area)
        {
        <% for_each_record | "Area" as AREA %>
        case Area.<%% $(AREA).name %%> :
        {
        <% if | !empty($(AREA).nodeFile) %>
            allNodes = this.sourceStream;
        <% else %>
            string allNodesFileName = "Resources/OSM/";
            allNodesFileName += <%% str | $(AREA).nodeFile %%>;
            allNodesFileName += ".osm.pbf";
            var allNodesInput = File.OpenRead(allNodesFileName);
            if (allNodesInput == null)
            {
                Debug.LogError("opening stream failed");
                return;
            }

            allNodes = new PBFOsmStreamSource(allNodesInput);
        <% end %>

            break;
        }
        <% end %>
        }

        this.ImportArea(allNodes);
    }

    void ImportArea(PBFOsmStreamSource allNodes)
    {
        switch (this.area)
        {
    <% for_each_record | "Area" as AREA %>
        case Area.<%% $(AREA).name %%> :
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
                <% if | !ne($(AREA).boundary.relationName, "") %>
                    string boundaryName = <%% str | $(AREA).boundary.relationName %%>;
                <% else %>
                    string boundaryName = <%% str | $(AREA).name %%>;
                <% end %>
                    if (tags.Contains("name", boundaryName))
                    {
                        importer.boundary = geo as Relation;
                        AddGeoReference(geo);
                    }
                }

                // Check parks.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
                <% for_each | $(AREA).nature as PARK %>
                    <% invoke if_tags_match($(PARK).tags) %>
                    {
                        <% if | $(PARK).visualOnly %>
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        <% end %>
                        importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(geo, NaturalFeature.Type.<%% $(PARK).type %%>));
                        AddGeoReference(geo);
                    }
                <% end %>
                }

                // Check buildings.
                if (geo.Type == OsmGeoType.Way || geo.Type == OsmGeoType.Relation)
                {
                <% for_each | $(AREA).buildings as BUILDING %>
                    <% invoke if_tags_match($(BUILDING).tags) %>
                    {
                        <% if | $(BUILDING).visualOnly %>
                        importer.visualOnlyFeatures.Add(geo.Id.Value);
                        <% end %>
                        importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(geo, Building.Type.<%% $(BUILDING).type %%>));
                        AddGeoReference(geo);
                    }
                <% end %>
                }

                // Check transit lines.
                if (geo.Type == OsmGeoType.Relation)
                {
                    while (true)
                    {
                        TransitType type;
                    <% for_each | $(AREA).transitLines as LINE_TYPE, i %>
                        <% if | !gt($(i), 0) %> else <% end %>
                        <% invoke if_tags_match($(LINE_TYPE).tags) %>
                        {
                            type = TransitType.<%% $(LINE_TYPE).type %%>;
                        }
                    <% end %>
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
                <% for_each | $(AREA).transitStops as STOP_TYPE, i %>
                    <% if | !gt($(i), 0) %> else <% end %>
                    <% invoke if_tags_match($(STOP_TYPE).tags) %>
                    {
                        Debug.Assert(geo.Id.HasValue, "stop does not have an ID");
                        importer.stops.Add(geo.Id.Value, geo as Node);
                        AddGeoReference(geo);
                    }
                <% end %>
                }

                // Check streets.
                if (geo.Type == OsmGeoType.Way)
                {
                <% for_each | $(AREA).streets as STREET_TYPE, i %>
                    <% if | !gt($(i), 0) %> else <% end %>
                    <% invoke if_tags_match($(STREET_TYPE).tags) %>
                    {
                        importer.streets.Add(new Tuple<Way, Street.Type>(geo as Way, Street.Type.<%% $(STREET_TYPE).type %%>));
                        AddGeoReference(geo);
                    }
                <% end %>
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
    <% end %>
        }

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

#endif
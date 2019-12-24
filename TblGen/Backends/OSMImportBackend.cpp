
#include <tblgen/Record.h>
#include <tblgen/Value.h>

#include <llvm/ADT/ArrayRef.h>
#include <llvm/Support/Casting.h>
#include <llvm/Support/raw_ostream.h>

#include <iostream>
#include <string>
#include <vector>

using namespace tblgen;

namespace
{

class OSMImportBackend
{
   std::ostream &OS;
   RecordKeeper &RK;
   std::vector<Record*> areas;

   std::vector<std::string> nodeCode;
   std::vector<std::string> wayCode;
   std::vector<std::string> relationCode;

   template <class CallbackFn>
   void CheckTags(llvm::ArrayRef<Value *> tags, const CallbackFn &fn)
   {
      CheckTags(OS, tags, fn);
   }

   template <class CallbackFn>
   static void CheckTags(llvm::raw_ostream &OS, llvm::ArrayRef<Value *> tags, const CallbackFn &fn)
   {
      OS << "if (";

      int i = 0;
      for (auto *tagVal : tags)
      {
         if (i++ != 0)
            OS << " && ";

         auto *tag = llvm::cast<RecordVal>(tagVal)->getRecord();
         auto value = llvm::cast<StringLiteral>(
                          tag->getFieldValue("value"))
                          ->getVal();

         if (value.empty())
         {
            OS
                << "tags.ContainsKey(\""
                << llvm::cast<StringLiteral>(tag->getFieldValue("key"))->getVal()
                << "\")";
         }
         else
         {
            OS
                << "tags.Contains(\""
                << llvm::cast<StringLiteral>(tag->getFieldValue("key"))->getVal()
                << "\", \"" << value << "\")";
         }
      }

      OS << ") {\n";
      fn();
      OS << "\n}";
   }

   template <class CallbackFn>
   void ForEachArea(const CallbackFn &fn)
   {
      OS << "switch (this.area) {\n";
      for (Record *area : areas)
      {
         OS << "    case Area." << area->getName() << ": {\n";
         fn(area);
         OS << "        }\n        break;";
      }

      OS << "}\n";
   }

   void EmitImportTransitLines(Record *area);
   void EmitImportStreets(Record *area);
   void EmitBoundary(Record *area);
   void EmitParks(Record *area);
   void EmitBuildings(Record *area);

public:
   OSMImportBackend(std::ostream &os, RecordKeeper &rk)
       : OS(os), RK(rk)
   {
   }

   void Emit();
};

} // anonymous namespace

void OSMImportBackend::Emit()
{
   RK.getAllDefinitionsOf("Area", areas);

   OS << R"__(
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
)__";

   for (auto *area : areas)
   {
      OS << area->getName() << ",\n";
   }

   OS << R"__(
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
)__";

   ForEachArea([&](Record *area) {
      auto allNodesFile = llvm::cast<StringLiteral>(area->getFieldValue("nodeFile"))->getVal();
      if (allNodesFile == "")
      {
         OS << "allNodes = this.sourceStream;\n";
      }
      else
      {
         OS << R"__(
               string allNodesFileName;
               allNodesFileName = "Resources/OSM/";
               allNodesFileName += ")__"
            << allNodesFile << R"__(";
               allNodesFileName += ".osm.pbf";

               var allNodesInput = File.OpenRead(allNodesFileName);
               if (allNodesInput == null)
               {
                     Debug.LogError("opening stream failed");
                     return;
               }

               allNodes = new PBFOsmStreamSource(allNodesInput);
)__";
      }
   });

   OS << R"__(

       this.ImportArea(allNodes);
    }

    void ImportArea(PBFOsmStreamSource allNodes)
    {
)__";

   ForEachArea([&](Record *area) {
      EmitBoundary(area);
      EmitImportTransitLines(area);
      EmitImportStreets(area);
      EmitParks(area);
      EmitBuildings(area);

      OS << R"__(
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
                  
)__";

      int i = 0;
      for (auto &code : relationCode)
      {
         if (i++ != 0)
            OS << "\n";
         OS << "while (true) {\n"
            << code << "\nbreak;\n}\n";
      }

      OS << R"__(
               break;
               case OsmGeoType.Way:
                  var way = geo as Way;
)__";

      i = 0;
      for (auto &code : wayCode)
      {
         if (i++ != 0)
            OS << "\n";
         OS << "while (true) {\n"
            << code << "\nbreak;\n}\n";
      }

      OS << R"__(
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

)__";

      i = 0;
      for (auto &code : nodeCode)
      {
         if (i++ != 0)
            OS << "\n";
         OS << "while (true) {\n"
            << code << "\nbreak;\n}\n";
      }

      OS << R"__(
               break;
            }
         }
)__";

      relationCode.clear();
      wayCode.clear();
      nodeCode.clear();
   });

   OS << R"__(
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
)__";
}

void OSMImportBackend::EmitBoundary(Record *area)
{
   std::string &relation = relationCode.emplace_back();
   llvm::raw_string_ostream REL(relation);

   auto boundary = llvm::cast<RecordVal>(area->getFieldValue("boundary"))->getRecord();
   auto tags = llvm::cast<ListLiteral>(boundary->getFieldValue("tags"))->getValues();
   auto wayTags = llvm::cast<ListLiteral>(boundary->getFieldValue("wayTags"))->getValues();

   auto name = llvm::cast<StringLiteral>(boundary->getFieldValue("name"))->getVal();
   auto relationName = llvm::cast<StringLiteral>(boundary->getFieldValue("relationName"))->getVal();

   if (!relationName.empty())
   {
      name = relationName;
   }

   if (name.empty())
   {
      return;
   }

   REL << "if (tags.Contains(\"name\", \"" << name << "\")) {";
   CheckTags(REL, tags, [&]() {
      REL << "                importer.boundary = geo as Relation; AddGeoReference(geo); break;\n";
   });
   REL << "}";
}

void OSMImportBackend::EmitImportTransitLines(Record *area)
{
   std::string &relation = relationCode.emplace_back();
   llvm::raw_string_ostream REL(relation);

   REL << R"__(
         TransitType type;
)__";

   auto lines = llvm::cast<ListLiteral>(area->getFieldValue("transitLines"))
                    ->getValues();

   int i = 0;
   for (auto *val : lines)
   {
      if (i++ != 0)
         REL << " else ";

      auto *line = llvm::cast<RecordVal>(val)->getRecord();
      auto tags = llvm::cast<ListLiteral>(line->getFieldValue("tags"))
                      ->getValues();

      auto type = llvm::cast<EnumVal>(
                      line->getFieldValue("type"))
                      ->getCase()
                      ->caseName;

      CheckTags(REL, tags, [&]() {
         REL << "type = TransitType." << type << ";";
      });
   }

   if (lines.empty())
   {
      REL << "                     if (false) {} ";
   }

   REL << R"__(
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
)__";

   std::string &node = nodeCode.emplace_back();
   llvm::raw_string_ostream NODE(node);

   auto stops = llvm::cast<ListLiteral>(area->getFieldValue("transitStops"))
                    ->getValues();

   i = 0;
   for (auto *val : stops)
   {
      if (i++ != 0)
         NODE << " else ";

      auto *stop = llvm::cast<RecordVal>(val)->getRecord();
      auto tags = llvm::cast<ListLiteral>(stop->getFieldValue("tags"))
                      ->getValues();

      CheckTags(NODE, tags, [&]() {
         NODE << "Debug.Assert(geo.Id.HasValue, \"stop does not have an ID\");"
                 "importer.stops.Add(geo.Id.Value, node); AddGeoReference(geo);";
      });
   }
}

void OSMImportBackend::EmitImportStreets(Record *area)
{
   std::string &way = wayCode.emplace_back();
   llvm::raw_string_ostream WAY(way);

   auto streets = llvm::cast<ListLiteral>(area->getFieldValue("streets"))
                      ->getValues();

   int i = 0;
   for (auto *val : streets)
   {
      if (i++ != 0)
         WAY << " else ";
      auto *street = llvm::cast<RecordVal>(val)->getRecord();
      auto type = llvm::cast<EnumVal>(street->getFieldValue("type"))->getCase()->caseName;
      auto tags = llvm::cast<ListLiteral>(street->getFieldValue("tags"))
                      ->getValues();

      CheckTags(WAY, tags, [&]() {
         WAY << "importer.streets.Add(new Tuple<Way, Street.Type>(way, Street.Type."
             << type << ")); AddGeoReference(way);";
      });
   }
}

void OSMImportBackend::EmitParks(Record *area)
{
   std::string &relation = relationCode.emplace_back();
   llvm::raw_string_ostream REL(relation);

   std::string &way = wayCode.emplace_back();
   llvm::raw_string_ostream WAY(way);

   auto parks = llvm::cast<ListLiteral>(area->getFieldValue("nature"))->getValues();
   for (auto *val : parks)
   {
      auto *park = llvm::cast<RecordVal>(val)->getRecord();
      auto type = llvm::cast<EnumVal>(park->getFieldValue("type"))->getCase()->caseName;
      auto tags = llvm::cast<ListLiteral>(park->getFieldValue("tags"))
                      ->getValues();
      auto geoTypes = llvm::cast<ListLiteral>(park->getFieldValue("geoTypes"))->getValues();

      for (auto *geoType : geoTypes)
      {
         auto name = llvm::cast<EnumVal>(geoType)->getCase()->caseName;
         if (name == "Relation")
         {
            CheckTags(REL, tags, [&]() {
               REL << "importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(rel, NaturalFeature.Type." << type << "));\n"
                   << "AddGeoReference(rel);";
            });
         }
         else if (name == "Way")
         {
            CheckTags(WAY, tags, [&]() {
               WAY << "importer.naturalFeatures.Add(new Tuple<OsmGeo, NaturalFeature.Type>(way, NaturalFeature.Type." << type << "));\n"
                   << "AddGeoReference(way);";
            });
         }
      }
   }
}

void OSMImportBackend::EmitBuildings(Record *area)
{
   std::string &relation = relationCode.emplace_back();
   llvm::raw_string_ostream REL(relation);

   std::string &way = wayCode.emplace_back();
   llvm::raw_string_ostream WAY(way);

   auto buildings = llvm::cast<ListLiteral>(area->getFieldValue("buildings"))->getValues();
   for (auto *val : buildings)
   {
      auto *building = llvm::cast<RecordVal>(val)->getRecord();
      auto type = llvm::cast<EnumVal>(building->getFieldValue("type"))->getCase()->caseName;
      auto tags = llvm::cast<ListLiteral>(building->getFieldValue("tags"))
                      ->getValues();
      auto geoTypes = llvm::cast<ListLiteral>(building->getFieldValue("geoTypes"))->getValues();

      for (auto *geoType : geoTypes)
      {
         auto name = llvm::cast<EnumVal>(geoType)->getCase()->caseName;
         if (name == "Relation")
         {
            CheckTags(REL, tags, [&]() {
               REL << "importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(rel, Building.Type." << type << "));\n"
                   << "AddGeoReference(rel);";
            });
         }
         else if (name == "Way")
         {
            CheckTags(WAY, tags, [&]() {
               WAY << "importer.buildings.Add(new Tuple<OsmGeo, Building.Type>(way, Building.Type." << type << "));\n"
                   << "AddGeoReference(way);";
            });
         }
      }
   }
}

extern "C"
{
   void EmitOSMImport(std::ostream &OS, RecordKeeper &RK)
   {
      OSMImportBackend(OS, RK).Emit();
   }
};
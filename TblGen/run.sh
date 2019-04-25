
cmake .
make

tblgen Files/OSMImport.tg -OSMImport libtransidious-tblgens.dylib > ../Assets/Scripts/OSM/OSMImportHelper.cs

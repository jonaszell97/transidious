
cmake .
make

# tblgen Files/OSMImport.tg -OSMImport ../TblGen/libtransidious-tblgens.dylib > ../Assets/Scripts/OSM/OSMImportHelper.cs
tblgen Files/OSMImport.tg -t Backends/OSMImportHelper.template.cs > ../Assets/Scripts/OSM/OSMImportHelper.cs

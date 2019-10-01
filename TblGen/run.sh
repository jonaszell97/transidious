
cmake .
make

$TBLGEN_PATH/tblgen Files/OSMImport.tg -OSMImport ../TblGen/libtransidious-tblgens.dylib > ../Assets/Scripts/OSM/OSMImportHelper.cs

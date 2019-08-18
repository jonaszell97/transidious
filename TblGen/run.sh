
cmake .
make

tblgen Files/OSMImport.tg -OSMImport ../TblGen/libtransidious-tblgens.so > ../Assets/Scripts/OSM/OSMImportHelper.cs


cmake .
make

tblgen Files/OSMImport.tg -OSMImport ~/transidious/TblGen/libtransidious-tblgens.so > ../Assets/Scripts/OSM/OSMImportHelper.cs


import os
import platform
import requests
import subprocess
import sys
from xml.etree import ElementTree

api = "https://api.openstreetmap.org/"

searchTerm = sys.argv[1]
searchResult = requests.get("http://open.mapquestapi.com/nominatim/v1/search.php", params={
    'key': 'mWyl2BDPDvj1mqGywsl01UYTGcGTGIY7',
    'format': 'json',
    'q': searchTerm,
})

results = searchResult.json()
results = [res for res in results if res['osm_type'] == 'relation']

if len(results) == 0:
    print('no results found')
    exit(1)

if len(results) > 1:
    print('multiple results found, please choose one')

    i = 0
    for result in results:
        print('   ' + str(i) + ' ' + result['osm_id'] + ' ' + result['display_name'])
        i += 1

    idx = input('choice: ')
    result = results[int(idx)]
else:
    result = results[0]

id = result['osm_id']
searchTerm = searchTerm.replace(' ', '')

if len(sys.argv) > 2:
    country = sys.argv[2]
else:
    country = result['display_name'].split(', ')[-1]

polyResult = requests.get("http://polygons.openstreetmap.fr/get_poly.py", params={
    'id': id,
    'params': 0,
})

polyFileName = '../Resources/Poly/' + searchTerm + '.poly'
polyFile = open(polyFileName, 'w')
polyFile.write(polyResult.text.replace('\t', '  '))
polyFile.close()

# generate pbf file from polygon
if not os.path.exists('../Resources/OSM/' + country):
    os.mkdir('../Resources/OSM/' + country)

pbfFileName = '../Resources/OSM/' + country + '/' + searchTerm + '.osm.pbf'
if not os.path.isfile(pbfFileName):
    FNULL = open(os.devnull, 'w')
    subprocess.run(
        [
            'osmosis',
            '--read-pbf', '../Resources/OSM/' + country + '.osm.pbf',
            '--bounding-polygon', 'file=' + os.path.abspath(polyFileName) + '',
            '--write-pbf', pbfFileName,
        ] #, stdout=FNULL, stderr=subprocess.STDOUT
    )

# output tblgen suggestion
infoResult = requests.get('https://www.openstreetmap.org/api/0.6/relation/' + id)
tree = ElementTree.fromstring(infoResult.content)

admin_level = 0
boundary = 'administrative'
rel = tree.find('relation')

for child in rel:
    if child.tag == 'tag':
        key = child.attrib['k']
        if key == 'admin_level':
            admin_level = child.attrib['v']
        elif key == 'boundary':
            boundary = child.attrib['v']

tgFile = open('./Files/OSMImport.tg', 'r')
tgContent = tgFile.read()
tgFile.close()

tgBackup = open('./Files/_OSMImport.tg', 'w')
tgBackup.write(tgContent)
tgBackup.close()

index = tgContent.find('def ' + searchTerm)
if index != -1:
    open_braces = 0
    close_braces = 0

    beginIndex = index
    while (open_braces == 0 or open_braces != close_braces) and index < len(tgContent):
        if tgContent[index] == '{':
            open_braces += 1
        elif tgContent[index] == '}':
            close_braces += 1
        elif tgContent[index] == '\n' and open_braces == 0:
            break

        index += 1

    tgContent = tgContent[:beginIndex] + tgContent[index:]

code = """\
def {name} : DefaultArea {{
    country = "{country}"
    boundary = BoundaryInfo<"{name}", [
        Tag<"type", "boundary">,
        Tag<"admin_level", "{admin_level}">
    ], [
        Tag<"boundary", "{boundary}">,
        Tag<"admin_level", "{admin_level}">
    ]>
}}\
""".format(name=searchTerm, country=country, boundary=boundary, 
           admin_level=admin_level)

if tgContent[-1] != '\n':
    tgContent += '\n\n'

tgContent += code

outFile = open('./Files/OSMImport.tg', 'w')
outFile.write(tgContent)
outFile.close()

# regenerate the files
subprocess.run(['cmake', '.'])
subprocess.run(['make'])

if platform.system() == 'Darwin':
    dylibExt = 'dylib'
else:
    dylibExt = 'so'

subprocess.run([
    'tblgen', './Files/OSMImport.tg',
    '-OSMImport', './libtransidious-tblgens.' + dylibExt,
], stdout=open('../Assets/Scripts/OSM/OSMImportHelper.cs', 'w'))

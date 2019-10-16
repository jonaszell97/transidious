
import os
import platform
import requests
import subprocess
import sys
import io
import json
import numpy as np
import pyclipper as clip
from xml.etree import ElementTree

import matplotlib.pyplot as plt

def perpendicular(a):
    return np.array([a[1], -a[0]])

def normalize(v):
    return v / np.linalg.norm(v)

def scale_polygon(delta, vertices):
    pco = clip.PyclipperOffset()
    scaleFactor = 1000000

    subj = tuple(
        map(lambda v: (int(v[0] * scaleFactor), int(v[1] * scaleFactor)), vertices))

    pco.AddPath(subj, clip.JT_SQUARE, clip.ET_CLOSEDPOLYGON)
    result = pco.Execute(delta * scaleFactor)

    result = list(map(lambda t: np.array([float(t[0]) / scaleFactor, float(t[1] / scaleFactor)]), result[0]))
    result.append(result[0])

    return result

def display_poly(vertices, color):
    xs, ys = zip(*vertices)
    plt.plot(xs, ys, color)

# api = "https://api.openstreetmap.org/"

searchTerm = sys.argv[1]
cacheFile = ".cache/" + searchTerm

if os.path.isfile(cacheFile):
    print("loading from cache...")
    cache = open(cacheFile, 'r')
    results = json.loads(cache.read())
else:
    print("querying...")
    searchResult = requests.get("http://open.mapquestapi.com/nominatim/v1/search.php", params={
        'key': 'mWyl2BDPDvj1mqGywsl01UYTGcGTGIY7',
        'format': 'json',
        'q': searchTerm,
    })

    results = searchResult.json()
    results = [res for res in results if res['osm_type'] == 'relation']

    cache = open(cacheFile, 'w')
    cache.write(json.dumps(results))
    cache.close()

if len(results) == 0:
    print('no results found')
    exit(1)

if len(results) > 1:
    print('multiple results found, please choose one')

    i = 0
    for result in results:
        print('   ' + str(i) + ' ' +
              result['osm_id'] + ' ' + result['display_name'])
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

polyData = polyResult.text.replace('\t', '  ')
vertices = list()

polyLines = polyData.split("\n")[2:-2]
for line in polyLines:
    values = line.strip().split("  ")

    try:
        x = float(values[0])
        y = float(values[1])
    except:
        break

    vertices.append(np.array([x, y]))

###

# display_poly(vertices, 'r')
# display_poly(scale_polygon(.0001, vertices), 'g')
# plt.show()

# exit(0)

###

scaledPoly = scale_polygon(.0001, vertices)
scaledPolyStr = 'polygon\n1\n'

for pt in scaledPoly:
    scaledPolyStr += '  '
    scaledPolyStr += str(pt[0])
    scaledPolyStr += '  '
    scaledPolyStr += str(pt[1])
    scaledPolyStr += '\n'

scaledPolyStr += 'END\nEND\n'
polyData = scaledPolyStr

polyFileName = '../Resources/Poly/' + searchTerm + '.poly'
polyFile = open(polyFileName, 'w')
polyFile.write(polyData)
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
        ]  # , stdout=FNULL, stderr=subprocess.STDOUT
    )

# output tblgen suggestion
infoResult = requests.get(
    'https://www.openstreetmap.org/api/0.6/relation/' + id)
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

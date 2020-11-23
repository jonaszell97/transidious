#!/usr/bin/python3

import io
import sys
import re
newver = sys.argv[1]

file_name = "/Applications/Unity/Hub/Editor/%s/Unity.app/Contents/MonoBleedingEdge/etc/mono/config" % (newver)
file_contents = open(file_name, "rt")

regex = re.compile(r'<dllmap dll="([^"]+)" target="[^"]+"', re.IGNORECASE)
result = ""

for line in file_contents:
    result += regex.sub(r'<dllmap dll="\1" target="/Library/Frameworks/Mono.framework/Versions/6.6.0/lib/libgdiplus.0.dylib"', line)

file_contents.close()

file_contents = open(file_name, "wt")
file_contents.write(result)
file_contents.close()


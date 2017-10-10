#! /usr/bin/python

# ***********************************************************************
# Copyright (c) 2017 Unity Technologies. All rights reserved.
#
# Licensed under the ##LICENSENAME##.
# See LICENSE.md file in the project root for full license information.
# ***********************************************************************


# Read the specified file and replace all instances of searchstr with replacestr.

import os
import sys
import re

if len(sys.argv) < 4:
  sys.stderr.write("USAGE: {} input/path/to/file searchstr replacestr\n".format(sys.argv[0]))
  sys.stderr.write("Performs a search replace on the input file./.\n")
  sys.exit(1)

filename = sys.argv[1]
if not os.path.exists(filename):
  sys.stderr.write("ERROR: file {} does not exist\n".format(filename))
  sys.exit(1)
  
searchstr = sys.argv[2]
replacestr = sys.argv[3]
if not searchstr or not replacestr:
  sys.stderr.write("ERROR: invalid search or replace string detected")
  sys.exit(1)

with open(filename, 'r+') as filein:
  data=filein.read()
  data=re.sub(searchstr, replacestr, data, 1, re.IGNORECASE)
  filein.seek(0)
  filein.write(data)
  filein.truncate()
# MakeYamlToc
import re
import os.path
from os import path


#
#   Reads through the file line by line and turns the MD input into YAML output:
#
#       * [display](href)           =>  - name: display
#                                         href: href
#       <indent>* [display](href)   =>    items:
#                                         - name: display
#                                           href: href
#
def ConvertTOC(infile) :
    # storage
    yamlstream = ""
    plevel = 0
    level = 0
    indent = ""
    tab = ""
    newtab = "  "
    
    # action
    f = open(infile, "r")
    for line in f :
        r = re.search("^(\s*?)\* \[(.*?)\]\((.*?)\).*?$", line)
        if r != None :
            # get matches
            indent = r.group(1)
            display = r.group(2)
            href = r.group(3)
            
            if indent != "" and tab == "" :
                # set tab character (first time only)
                tab = indent
                level = 1
            elif indent == "" :
                # no indent (use two spaces)
                level = 0
            else :
                # compare current match to tab to calculate level
                m = re.findall(tab, indent)
                level = len(m)
            
            # analysis
            indent = newtab * level
            if level > plevel :
                pindent = newtab * plevel
                yamlstream += "\n"+pindent+"  items:\n"+indent+"- name: "+display+"\n"+indent+"  href: "+href
            else :
                yamlstream += "\n"+indent+"- name: "+display+"\n"+indent+"  href: "+href
            
            # prime the loop
            plevel = level
    
    # send back the results (minus the leading line break)
    f.close()
    return yamlstream.lstrip("\n")


#
# Main / launch point
#
tocstream = ""
mdfile = "TableOfContents.md"
if path.exists(mdfile) :
    tocstream = ConvertTOC("TableOfContents.md")
else :
    mdfile = raw_input("Enter the location of the TableOfContents.md file you want to convert: ")
    if mdfile == "" :
        print("Can't continue. You need to specify the location of the MD toc file (eg., `TableOfContents.md`)")
    else :
        if path.exists(mdfile) and path.isfile(mdfile) :
            print("Processing "+mdfile+"...")
            tocstream = ConvertTOC(path.abspath(mdfile))
        elif not(path.exists(mdfile)) :
            print("Can't continue. "+mdfile+" doesn't exist.")
        else :
            print("Can't continue. "+mdfile+" isn't a file.")

if tocstream == "" :
    print("Failed to find convert the "+mdfile+" file to YAML.")
else :
    print("Finished creating toc.yml file from "+mdfile+".")
    basedir = path.dirname(path.abspath(mdfile))
    outfile = path.join(basedir, "toc.yml")
    o = open(outfile, "w")
    o.write(tocstream)
    o.close()

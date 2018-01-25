import xml.etree.ElementTree as ET
import os
def splitxml(nsize,value):
    context = ET.iterparse('D:/code coverage/xml/code08182017030354.xml', events=('end', ))
    output = "D:/del2/"
    startxml = """<?xml version="1.0" encoding="utf-8"?>
        <CoverageSession xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
          <Modules>
           """
    endxml = """  </Modules>
        </CoverageSession>"""
    index = 0
    temp = 0
    tempname = ""
    for event, elem in context:
        if elem.tag == 'Module':
            filename = format(output + value + str(index) + ".xml")
            with open(filename, 'ab') as f:
                if(index == temp):                
                   f.write(startxml.encode())
                f.write(ET.tostring(elem))
                elem.clear()
            temp = -1
    ##        tempname = filename
            print (filename)
            fileInfo = os.stat(filename)
            fileSize = fileInfo.st_size
            file = fileSize >> 20
            if(file >= nsize):
                index += 1
                temp = index
    ##            tempname = ""
                with open(filename, 'ab') as f:
                    f.write(endxml.encode())
    ##if(tempname != ""):
    ##    with open(tempname, 'ab') as f:
    ##        f.write(endxml.encode())





from tkinter import *
from tkinter.ttk import *
from tkinter import filedialog as fd
import xml.etree.ElementTree as ET
import os

root = Tk()
root.title("Split Xml")
root.minsize(width=300, height=130)
root.maxsize(width=300, height=130)
Label(root, text = "XML File Path!").grid(row = 0, sticky = W)
Label(root, text = "Chunk size!").grid(row = 1, sticky = W)
Label(root, text = "Output Folder!").grid(row = 2, sticky = W)
path = Entry(root)
size = Entry(root)
outpath = Entry(root)
path.grid(row = 0, column = 1)
size.grid(row = 1, column = 1)
outpath.grid(row = 2, column = 1)

def getxmlpath():
    filepath = str(fd.askopenfilename(initialdir=os.getcwd(), title="Select CodeCoverage XML file",
                                           filetypes=[("XML Files", "*.xml")]))
    path.delete(0,END)
    path.insert(0,filepath)
def getoutpath():
    outputpath = str(fd.askdirectory())
    outpath.delete(0,END)
    outpath.insert(0,outputpath + '/')
def splitxml():
    file = path.get()
    nsize = int(size.get())
    output = outpath.get() 
    #root.destroy()
    global params
    params = [file,nsize,output]
    context = ET.iterparse(file, events=('end', ))
    value = ''
    startxml = """<?xml version="1.0" encoding="utf-8"?>
        <CoverageSession xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
          <Modules>
           """
    endxml = """  </Modules>
        </CoverageSession>"""
    index = 0
    temp = 0
    flag = ""
    for event, elem in context:
        if elem.tag == 'Module':
            filename = format(output + value + str(index) + ".xml")
            with open(filename, 'ab') as f:
                if(index == temp):                
                   f.write(startxml.encode())
                f.write(ET.tostring(elem))
                elem.clear()
            temp = -1
            flag = filename
            fileInfo = os.stat(filename)
            fileSize = fileInfo.st_size
            file = fileSize >> 20
            if(file >= nsize):
                index += 1
                temp = index
                flag = ""
                with open(filename, 'ab') as f:
                    f.write(endxml.encode())
    if(flag != ""):
        with open(flag, 'ab') as f:
            f.write(endxml.encode())
Button(root, text="Browse", command=getxmlpath).grid(row = 0, column = 2)
Button(root, text="Browse", command=getoutpath).grid(row = 2, column = 2)
Button(root, text = "Generate",command = splitxml).grid(row = 4,column = 1, sticky = N)
mainloop()

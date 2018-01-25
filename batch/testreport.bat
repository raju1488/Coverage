@echo off
set "targetdir=D:\code coverage\xml\report33\"
set "reports=\\ldo-pkt-pc1150\Share\IIS\xml\test2.xml"
cd\
cd "D:\code coverage\ReportGenerator-2.5.8.0\ReportGenerator-2.5.8.0\ReportGenerator\bin\Debug"
echo on
ReportGenerator.exe -reports:"%reports%" -targetdir:"%targetdir%"
pause
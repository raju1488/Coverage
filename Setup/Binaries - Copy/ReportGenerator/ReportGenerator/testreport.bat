@echo off
set "targetdir=D:\code coverage\test report\26"
REM D:\code coverage\test report\23

set "reports=D:\del1\*.xml"
REM set "reports=D:\del4\*.xml"
REM set "reports=D:\code coverage\xml\test.xml" -reporttypes:"HtmlSummary"
cd\
cd "D:\code coverage\RG\New folder\ReportGenerator-2.5.10.0\ReportGenerator\bin\Debug"
echo on
ReportGenerator.exe -reports:"%reports%" -targetdir:"%targetdir%" -reporttypes:"HtmlSummary"
pause
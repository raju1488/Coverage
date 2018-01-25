@echo off
set "iis=C:\Windows\System32\inetsrv\w3wp.exe"
set "targetdir=C:\i18n\i18Host\WebClient\bin"
set "output=C:\i18n\i18Host\test122.xml"
set "filter=+[*ResultManagement*]* +[*ResultManagementUI]* +[LorAppCommon]*"
set "info=-debug "
REM > "C:\Share\CodeCoverage\batch\" -searchdirs:"%searchdirs%" 
cd cd\
d:
cd C:\Share\CodeCoverage\batch\opencover.4.6.519
echo on
"OpenCover.Console.exe" -skipautoprops -register:user -target:"%iis%" -targetargs:"-debug -in test-1000" -targetdir:"%targetdir%" -filter:"%filter%" -output:"%output%" -nodefaultfilters 
REM "D:\code coverage\new\opencover-master\opencover-master\main\bin\Debug\OpenCover.Console.exe" -skipautoprops -register:user -target:"%iis%" -targetargs:"%info%" -targetdir:"%targetdir%" -filter:"%filter%" -output:"%output%" -nodefaultfilters 
pause
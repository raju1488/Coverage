set "installutil=C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe"
set "service=%~dp0\CodeServiceHost.exe
%installutil% %service%
pause
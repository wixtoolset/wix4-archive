@echo off
setlocal
set _P=%~dp1
del %TEMP%\foo.wixobj
%WIX_ROOT%\build\debug\x86\candle.exe %1 -out %TEMP%\foo.wixobj -ext WixNetFxExtension -ext WixUtilExtension
%WIX_ROOT%\build\debug\x86\lit.exe %TEMP%\foo.wixobj -out %_P%Expected-Wixlib.xml -ext WixNetFxExtension -ext WixUtilExtension
endlocal
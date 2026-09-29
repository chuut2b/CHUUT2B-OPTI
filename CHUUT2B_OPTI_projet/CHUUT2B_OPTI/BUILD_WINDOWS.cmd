@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
  echo .NET 8 SDK est requis pour compiler CHUUT2B OPTI.
  echo Installe le SDK .NET 8 : https://dotnet.microsoft.com/download/dotnet/8.0
  pause
  exit /b 1
)
dotnet publish CHUUT2B_OPTI.csproj -c Release -o publish
if errorlevel 1 (
  echo.
  echo ECHEC de la compilation. Copie les erreurs ci-dessus.
  pause
  exit /b 1
)
echo.
echo Termine : %~dp0publish\CHUUT2B_OPTI.exe
pause

@echo off
echo Starting NewYT Applications...
echo.

echo Starting Console Video Fetcher Service...
start "NewYT Console" cmd /k "cd newyt.console && dotnet run"

echo Waiting 3 seconds...
timeout /t 3 /nobreak > nul

echo Starting Web Application...
start "NewYT Web" cmd /k "cd newyt.web && dotnet run"

echo.
echo Both applications are starting...
echo.
echo Console Application: Check the "NewYT Console" window for logs
echo Web Application: Will be available at http://localhost:5216
echo.
echo Press any key to exit (applications will continue running)
pause > nul
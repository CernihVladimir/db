@echo off
setlocal
cd /d "%~dp0"

dotnet test Tests\Tests.csproj --logger "console;verbosity=normal"
if errorlevel 1 goto failed

dotnet test Tests\Tests.csproj --no-build --no-restore --logger "console;verbosity=normal" -- NUnit.Where="test == Tests.MongoUserRepositoryShould.SearchByLoginFast or test == Tests.MongoUserRepositoryShould.LoginDuplicateNotAllowed or test == Tests.MongoUserRepositoryShould.MassiveConcurrentCreateUser"
if errorlevel 1 goto failed

echo.
echo All tests passed.
pause
exit /b 0

:failed
echo.
echo Tests failed. See the output above.
pause
exit /b 1

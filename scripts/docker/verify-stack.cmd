@echo off
setlocal

set FUNCTIONS_CONTAINER=coffeenchill-functions
set AZURITE_CONTAINER=coffeenchill-azurite
set NETWORK_NAME=coffeenchill-network

echo.
echo ==========================================
echo CoffeeNChill Docker Stack Verification
echo ==========================================
echo.

echo [1/5] Checking Functions container...
docker inspect -f "{{.State.Running}}" %FUNCTIONS_CONTAINER% 2>nul | findstr /i "true" >nul

if errorlevel 1 (
    echo ERROR: Functions container is not running.
    exit /b 1
)

echo PASS: %FUNCTIONS_CONTAINER% is running.

echo.
echo [2/5] Checking Azurite container...
docker inspect -f "{{.State.Running}}" %AZURITE_CONTAINER% 2>nul | findstr /i "true" >nul

if errorlevel 1 (
    echo ERROR: Azurite container is not running.
    exit /b 1
)

echo PASS: %AZURITE_CONTAINER% is running.

echo.
echo [3/5] Checking Docker network...
docker network inspect %NETWORK_NAME% >nul 2>&1

if errorlevel 1 (
    echo ERROR: Docker network %NETWORK_NAME% was not found.
    exit /b 1
)

echo PASS: Docker network exists.

echo.
echo [4/5] Checking Functions HTTP endpoint...
for /f "delims=" %%S in ('curl -s -o NUL -w "%%{http_code}" http://localhost:7071/api/documents') do set FUNCTIONS_STATUS=%%S

echo HTTP status: %FUNCTIONS_STATUS%

if not "%FUNCTIONS_STATUS%"=="200" (
    echo ERROR: Functions HTTP endpoint did not return 200.
    exit /b 1
)

echo PASS: Functions HTTP endpoint returned 200.

echo.
echo [5/5] Checking Azurite Blob service...
for /f "delims=" %%S in ('curl -s -o NUL -w "%%{http_code}" http://localhost:10000/devstoreaccount1/') do set AZURITE_STATUS=%%S

echo HTTP status: %AZURITE_STATUS%

if "%AZURITE_STATUS%"=="200" (
    echo PASS: Azurite Blob service returned 200.
) else if "%AZURITE_STATUS%"=="400" (
    echo PASS: Azurite Blob service responded and rejected the unauthenticated request as expected.
) else if "%AZURITE_STATUS%"=="403" (
    echo PASS: Azurite Blob service responded and rejected the unauthenticated request as expected.
) else (
    echo ERROR: Unexpected Azurite Blob HTTP status.
    exit /b 1
)

echo.
echo ==========================================
echo Docker stack verification completed
echo ==========================================
echo.

endlocal
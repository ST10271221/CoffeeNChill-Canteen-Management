@echo off
setlocal

set CONTAINER_NAME=coffeenchill-azurite
set IMAGE_NAME=mcr.microsoft.com/azure-storage/azurite
set NETWORK_NAME=coffeenchill-network

echo.
echo ==========================================
echo CoffeeNChill Azurite Storage Emulator
echo ==========================================
echo.

docker image inspect %IMAGE_NAME% >nul 2>&1

if errorlevel 1 (
    echo Pulling the official Azurite image...
    docker pull %IMAGE_NAME%

    if errorlevel 1 (
        echo ERROR: Failed to pull the Azurite image.
        exit /b 1
    )
)

docker network inspect %NETWORK_NAME% >nul 2>&1

if errorlevel 1 (
    echo Creating Docker network %NETWORK_NAME%...

    docker network create %NETWORK_NAME%

    if errorlevel 1 (
        echo ERROR: Failed to create Docker network.
        exit /b 1
    )
)

docker container inspect %CONTAINER_NAME% >nul 2>&1

if not errorlevel 1 (
    echo Existing Azurite container found.

    docker start %CONTAINER_NAME% >nul 2>&1

    if errorlevel 1 (
        echo ERROR: Failed to start the existing Azurite container.
        exit /b 1
    )

    echo Azurite container started successfully.

    docker network inspect %NETWORK_NAME% --format "{{json .Containers}}" | findstr /i "%CONTAINER_NAME%" >nul 2>&1

    if errorlevel 1 (
        echo Connecting Azurite container to %NETWORK_NAME%...

        docker network connect %NETWORK_NAME% %CONTAINER_NAME%

        if errorlevel 1 (
            echo ERROR: Failed to connect Azurite to the Docker network.
            exit /b 1
        )
    )

    goto :verify
)

echo Creating a new Azurite container...

docker run -d ^
    --name %CONTAINER_NAME% ^
    --network %NETWORK_NAME% ^
    -p 10000:10000 ^
    -p 10001:10001 ^
    -p 10002:10002 ^
    %IMAGE_NAME%

if errorlevel 1 (
    echo ERROR: Failed to create the Azurite container.
    exit /b 1
)

echo Azurite container created successfully.

:verify

echo.
echo Verifying Azurite container...
echo.

for /f "delims=" %%S in ('docker inspect -f "{{.State.Status}}" %CONTAINER_NAME% 2^>nul') do set CONTAINER_STATUS=%%S

if not "%CONTAINER_STATUS%"=="running" (
    echo ERROR: Azurite container is not running.
    exit /b 1
)

docker ps --filter "name=%CONTAINER_NAME%"

echo.
echo Azurite services:
echo   Blob  Storage: http://localhost:10000
echo   Queue Storage: http://localhost:10001
echo   Table Storage: http://localhost:10002
echo.
echo Azurite is ready.
echo.

endlocal

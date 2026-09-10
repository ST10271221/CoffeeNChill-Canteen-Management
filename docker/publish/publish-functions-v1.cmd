@echo off
setlocal

set IMAGE_NAME=coffeenchill-functions
set SOURCE_TAG=optimized
set DOCKERHUB_IMAGE=st10271221/coffeenchill-functions
set RELEASE_TAG=v1.0

echo.
echo ==========================================
echo CoffeeNChill Functions Docker Release
echo ==========================================
echo.

echo Checking local optimized image...

docker image inspect %IMAGE_NAME%:%SOURCE_TAG% >nul 2>&1

if errorlevel 1 (
    echo ERROR: Local optimized image was not found.
    echo Build it first with:
    echo docker build -t %IMAGE_NAME%:%SOURCE_TAG% .
    exit /b 1
)

echo.
echo Tagging image:
echo   %IMAGE_NAME%:%SOURCE_TAG%
echo   %DOCKERHUB_IMAGE%:%RELEASE_TAG%
echo.

docker tag %IMAGE_NAME%:%SOURCE_TAG% %DOCKERHUB_IMAGE%:%RELEASE_TAG%

if errorlevel 1 (
    echo ERROR: Failed to tag Docker image.
    exit /b 1
)

echo Image tagged successfully.

echo.
echo Publishing image to Docker Hub...
echo.

docker push %DOCKERHUB_IMAGE%:%RELEASE_TAG%

if errorlevel 1 (
    echo.
    echo ERROR: Docker image push failed.
    exit /b 1
)

echo.
echo ==========================================
echo Docker release published successfully.
echo ==========================================
echo.
echo Image:
echo   %DOCKERHUB_IMAGE%:%RELEASE_TAG%
echo.

endlocal
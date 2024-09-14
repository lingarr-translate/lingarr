param (
    [string]$Tag = "latest",
    [string]$BuildConfiguration = "Release"
)

# Set variables
$IMAGE_NAME = "lingarr/lingarr"

# Build the Docker image
Write-Host "Building Docker image with BUILD_CONFIGURATION=$BuildConfiguration and tag=$Tag" -ForegroundColor Cyan
docker build --build-arg BUILD_CONFIGURATION=$BuildConfiguration . -t ${IMAGE_NAME}:$Tag -f Lingarr.Server/Dockerfile

# Check if build was successful
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker build failed. Please check the Dockerfile and error message above." -ForegroundColor Red
    exit 1
}

# Tag the image with today's date
$DATE_TAG = Get-Date -Format "yyyyMMdd"
docker tag ${IMAGE_NAME}:$Tag ${IMAGE_NAME}:$DATE_TAG

# Push the images to Docker Hub
docker push ${IMAGE_NAME}:$Tag
Write-Host "Pushing tag=$Tag image to Docker Hub" -ForegroundColor Cyan

# If the tag isn't 'latest', also tag and push as 'latest'
if ($Tag -ne "dev") {
    docker push ${IMAGE_NAME}:$DATE_TAG
    Write-Host "Also tagged and pushed as '$DATE_TAG'" -ForegroundColor Cyan
}

Write-Host "Build and push completed successfully" -ForegroundColor Green
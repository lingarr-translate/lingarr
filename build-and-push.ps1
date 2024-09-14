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


# Push the images to Docker Hub
if ($Tag -eq "dev") {
    # Push only the dev tag
    docker push ${IMAGE_NAME}:$Tag
    Write-Host "Pushing dev tag to Docker Hub" -ForegroundColor Cyan
} elseif ($Tag -ne "latest") {
    # Push the specified tag and latest
    docker push ${IMAGE_NAME}:$Tag
    docker tag ${IMAGE_NAME}:$Tag ${IMAGE_NAME}:latest
    docker push ${IMAGE_NAME}:latest
    Write-Host "Pushing $Tag and latest tags to Docker Hub" -ForegroundColor Cyan
} else {
    # Push only the latest tag
    docker push ${IMAGE_NAME}:latest
    Write-Host "Pushing latest tag to Docker Hub" -ForegroundColor Cyan
}

Write-Host "Build and push completed successfully" -ForegroundColor Green
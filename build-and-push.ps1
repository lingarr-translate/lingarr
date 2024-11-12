param (
    [string]$Tag = "latest",
    [string]$BuildConfiguration = "Release"
)

# Set variables
$IMAGE_NAME = "lingarr/lingarr"

# Create a new builder instance if it doesn't exist
Write-Host "Setting up docker buildx builder" -ForegroundColor Cyan
$builderExists = docker buildx ls | Select-String "multiplatform-builder"
if (!$builderExists) {
    docker buildx create --name multiplatform-builder --use
    docker buildx inspect --bootstrap
} else {
    docker buildx use multiplatform-builder
}

# Function to build for a specific platform
function Build-Platform {
    param (
        [string]$Platform,
        [string]$Tag,
        [string]$ArchSuffix
    )

    $fullTag = "${IMAGE_NAME}:${Tag}"
    if ($ArchSuffix -eq "arm64")
    {
        $fullTag = "${fullTag}-${ArchSuffix}"
    }
    Write-Host "Building for $Platform with tag $fullTag" -ForegroundColor Cyan

    docker buildx build --platform $Platform `
        --build-arg BUILD_CONFIGURATION=$BuildConfiguration `
        --load `
        --tag $fullTag `
        -f Lingarr.Server/Dockerfile .

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Successfully built $fullTag" -ForegroundColor Green
        docker push $fullTag
        Write-Host "Successfully pushed $fullTag" -ForegroundColor Green
    } else {
        Write-Host "Failed to build $fullTag" -ForegroundColor Red
        exit 1
    }
}

try {
    Write-Host "Building Docker images with BUILD_CONFIGURATION=$BuildConfiguration" -ForegroundColor Cyan

    if ($Tag -eq "dev") {
        # Build dev tags for each architecture
        Build-Platform -Platform "linux/arm64" -Tag "dev" -ArchSuffix "arm64"
        Build-Platform -Platform "linux/amd64" -Tag "dev" -ArchSuffix "amd64"
    } elseif ($Tag -ne "latest") {
        # Build specific version tags for each architecture
        Build-Platform -Platform "linux/arm64" -Tag $Tag -ArchSuffix "arm64"
        Build-Platform -Platform "linux/amd64" -Tag $Tag -ArchSuffix "amd64"

        # Also build latest tags for each architecture
        Build-Platform -Platform "linux/arm64" -Tag "latest" -ArchSuffix "arm64"
        Build-Platform -Platform "linux/amd64" -Tag "latest" -ArchSuffix "amd64"
    } else {
        # Build only latest tags for each architecture
        Build-Platform -Platform "linux/arm64" -Tag "latest" -ArchSuffix "arm64"
        Build-Platform -Platform "linux/amd64" -Tag "latest" -ArchSuffix "amd64"
    }

    Write-Host "Build and push completed successfully" -ForegroundColor Green
} catch {
    Write-Host "An error occurred during the build process: $_" -ForegroundColor Red
    exit 1
}
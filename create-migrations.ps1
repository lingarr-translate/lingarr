param (
    [Parameter(Mandatory=$true)]
    [string]$MigrationName
)

function Create-Migrations {
    param (
        [string]$Provider,
        [string]$ContextName,
        [string]$MigrationsProject,
        [string]$OutputDir
    )
    
    Write-Host "Creating $Provider migrations..." -ForegroundColor Cyan
    
    if ($Provider -eq "mysql") {
        $env:DB_CONNECTION = "mysql"
        $env:DB_HOST = "localhost"
        $env:DB_PORT = "1433"
        $env:DB_DATABASE = "LingarrMysql"
        $env:DB_USERNAME = "LingarrMysql"
        $env:DB_PASSWORD = "Secret1234"
    } else {
        Remove-Item Env:\DB_CONNECTION -ErrorAction SilentlyContinue
        Remove-Item Env:\DB_HOST -ErrorAction SilentlyContinue
        Remove-Item Env:\DB_PORT -ErrorAction SilentlyContinue
        Remove-Item Env:\DB_DATABASE -ErrorAction SilentlyContinue
        Remove-Item Env:\DB_USERNAME -ErrorAction SilentlyContinue
        Remove-Item Env:\DB_PASSWORD -ErrorAction SilentlyContinue
    }
    
    dotnet ef migrations add $MigrationName `
        --context $ContextName `
        --output-dir $OutputDir `
        --project $MigrationsProject `
        --startup-project Lingarr.Server
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error creating migrations for $Provider" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    Write-Host "$Provider migrations '$MigrationName' created successfully" -ForegroundColor Green
    Write-Host ""
}

# Create migrations for SQLite
Create-Migrations -Provider "sqlite" -ContextName "LingarrDbContext" -MigrationsProject "Lingarr.Migrations.SQLite" -OutputDir "Migrations"

# Create migrations for MySQL
Create-Migrations -Provider "mysql" -ContextName "LingarrDbContext" -MigrationsProject "Lingarr.Migrations.MySQL" -OutputDir "Migrations"

Write-Host "Migrations '$MigrationName' created successfully." -ForegroundColor Green
<#
.SYNOPSIS
    This script initiates SonarQube analysis for a .NET project, builds the project, and
    sends the results to the SonarQube server.

.DESCRIPTION
    The script automatically runs the three basic steps of the analysis
    (dotnet sonarscanner begin, dotnet build, dotnet sonarscanner end) using configurable variables.
    If the project build fails, the script automatically stops and does not run the 'end' step.
#>

# ------------------------------------------------------------------------------------
# --- Configuration Area ---
# Edit the variables in this section according to your project and server information.
# ------------------------------------------------------------------------------------

$projectKey   = "TodoKey"
$sonarHostUrl = "http://localhost:9001"
$sonarToken   = "sqp_60bf2870883421ed209ec07e0c44778173365915" # Paste your actual token code here
$solutionFile = "UseCase-Series.sln" # Enter the name of your project's .sln file here

# ------------------------------------------------------------------------------------
# --- Script Start (You don't need to edit this section) ---
# ------------------------------------------------------------------------------------

Clear-Host
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " Starting SonarQube .NET Analysis Script " -ForegroundColor Cyan
Write-Host "==============================================="

# Step 1: Start Analysis
Write-Host ""
Write-Host "1/3 - Running SonarScanner 'begin' step..." -ForegroundColor Yellow
dotnet sonarscanner begin /k:"$projectKey" /d:sonar.host.url="$sonarHostUrl" /d:sonar.login="$sonarToken"

# Check if the 'begin' command was successful
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] 'sonarscanner begin' step failed. Stopping script." -ForegroundColor Red
    exit 1 # Exit with error code
}

# Step 2: Build Project
Write-Host ""
Write-Host "2/3 - Building project: $solutionFile" -ForegroundColor Yellow
dotnet build $solutionFile

# Check if the build was successful
if ($LASTEXITCODE -eq 0) {
    Write-Host "[SUCCESS] Project build completed." -ForegroundColor Green

    # Step 3: Finish Analysis and Send Results
    Write-Host ""
    Write-Host "3/3 - Running SonarScanner 'end' step..." -ForegroundColor Yellow
    dotnet sonarscanner end /d:sonar.login="$sonarToken"

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================================================" -ForegroundColor Green
        Write-Host " SonarQube analysis completed successfully and results sent to server." -ForegroundColor Green
        Write-Host "========================================================================"
    } else {
        Write-Host "[ERROR] 'sonarscanner end' step failed. Please check the output." -ForegroundColor Red
    }

} else {
    Write-Host ""
    Write-Host "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" -ForegroundColor Red
    Write-Host "[ERROR] Project build FAILED. SonarQube analysis stopped and no results were sent to the server." -ForegroundColor Red
    Write-Host "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    exit 1 # Exit with error code
}
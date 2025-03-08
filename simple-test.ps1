# PowerShell script to test a simple PDF generation

$outputPath = "$PSScriptRoot\generated_pdfs"
$timestamp = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"
$outputFile = "$outputPath\SimpleTest_$timestamp.pdf"
$debugFile = "$PSScriptRoot\simple_test_debug.txt"

# Create directory if it doesn't exist
if (!(Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
    Write-Host "Created directory: $outputPath"
}

# Clear debug file
Set-Content -Path $debugFile -Value "Debug log started at $(Get-Date)`n"

# Simple JSON payload
$jsonBody = @{
    templateName = "SimpleTest"
    data = @{}
}

# Convert to JSON
$jsonContent = $jsonBody | ConvertTo-Json -Depth 10
Add-Content -Path $debugFile -Value "Request JSON: $jsonContent"

# Make the API request and save the response directly to a file
try {
    Write-Host "Sending request to generate simple PDF..."
    Add-Content -Path $debugFile -Value "Sending request to API at $(Get-Date)"
    
    # Use more detailed error handling
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5270/api/PDFGeneration/generate" `
                                    -Method Post `
                                    -ContentType "application/json" `
                                    -Body $jsonContent `
                                    -OutFile $outputFile `
                                    -ErrorVariable webError
        
        Add-Content -Path $debugFile -Value "Response received successfully"
        Write-Host "PDF successfully saved to: $outputFile"
        Write-Host "Debug log saved to: $debugFile"
        Write-Host "Opening file location..."
        
        # Open the folder containing the PDF
        Invoke-Item (Split-Path $outputFile -Parent)
    }
    catch [System.Net.WebException] {
        $statusCode = [int]$webError.Exception.Response.StatusCode
        $statusDesc = $webError.Exception.Response.StatusDescription
        $errorMsg = "HTTP Error: $statusCode - $statusDesc"
        
        if ($webError.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($webError.Exception.Response.GetResponseStream())
            $responseContent = $reader.ReadToEnd()
            $errorMsg += "`nResponse: $responseContent"
            Add-Content -Path $debugFile -Value "Error response content: $responseContent"
        }
        
        Write-Host "Error generating PDF: $errorMsg" -ForegroundColor Red
        Add-Content -Path $debugFile -Value "Error: $errorMsg"
    }
} catch {
    Write-Host "General error: $_" -ForegroundColor Red
    Add-Content -Path $debugFile -Value "General error: $_"
}

Write-Host "Debug information saved to: $debugFile"

# PowerShell script to download PDF from the API

$outputPath = "$PSScriptRoot\generated_pdfs"
$timestamp = Get-Date -Format "yyyy-MM-dd-HH-mm-ss"
$outputFile = "$outputPath\TestRazorDataAssessment_$timestamp.pdf"

# Create directory if it doesn't exist
if (!(Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
    Write-Host "Created directory: $outputPath"
}

# JSON payload
$jsonBody = @{
    templateName = "HealthAndWellness\TestRazorDataAssessment"
    data = @{
        user = @{
            id = 1
            name = "Alice Smith"
            email = "alice@example.com"
            is_active = $true
            created_at = "2023-03-01T12:34:56Z"
        }
        preferences = @{
            notifications = @{
                email = $true
                sms = $false
                push = $true
            }
            theme = "dark"
            language = "en-US"
        }
        orders = @(
            @{
                order_id = "ORD123"
                amount = 250.75
                items = @(
                    @{
                        item_id = "ITM1"
                        name = "Product 1"
                        quantity = 2
                        price = 50.25
                    },
                    @{
                        item_id = "ITM2"
                        name = "Product 2"
                        quantity = 1
                        price = 150.25
                    }
                )
                status = "Completed"
            },
            @{
                order_id = "ORD124"
                amount = 100.00
                items = @(
                    @{
                        item_id = "ITM3"
                        name = "Product 3"
                        quantity = 1
                        price = 100.00
                    }
                )
                status = "Pending"
            }
        )
    }
}

# Convert to JSON
$jsonContent = $jsonBody | ConvertTo-Json -Depth 10

# Make the API request and save the response directly to a file
try {
    Write-Host "Sending request to generate PDF..."
    $response = Invoke-RestMethod -Uri "http://localhost:5270/api/PDFGeneration/generate" `
                                -Method Post `
                                -ContentType "application/json" `
                                -Body $jsonContent `
                                -OutFile $outputFile
    
    Write-Host "PDF successfully saved to: $outputFile"
    Write-Host "Opening file location..."
    
    # Open the folder containing the PDF
    Invoke-Item (Split-Path $outputFile -Parent)
} catch {
    Write-Host "Error generating PDF: $_" -ForegroundColor Red
}

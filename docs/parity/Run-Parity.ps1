<#
.SYNOPSIS
  Captures or compares Carrier Test Harness responses for the fixed request set in requests.json.

.DESCRIPTION
  The Carrier Test Harness must already be running (default http://localhost:5085/) against the
  SANDBOX database. Every request is a Test Call (IsTest = true).

    capture : runs every request and saves the raw responses plus meta.json (pickup date, label).
    compare : runs every request again with the SAME pickup date as the baseline and compares
              the logged Carrier Rate Quotes against the baseline, ignoring ids and timings.

  Run both builds on the same day, close together: carrier rates can move.

.EXAMPLE
  .\Run-Parity.ps1 -Mode capture -OutDir .\out\net48 -Label net48
  .\Run-Parity.ps1 -Mode compare -BaselineDir .\out\net48 -OutDir .\out\net10 -Label net10
#>
param(
    [Parameter(Mandatory)][ValidateSet('capture', 'compare')][string]$Mode,
    [Parameter(Mandatory)][string]$OutDir,
    [string]$BaselineDir,
    [Parameter(Mandatory)][string]$Label,
    [string]$Endpoint = 'http://localhost:5085/test-request'
)

$ErrorActionPreference = 'Stop'
$requests = Get-Content (Join-Path $PSScriptRoot 'requests.json') -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

if ($Mode -eq 'compare') {
    if (-not $BaselineDir) { throw '-BaselineDir is required for compare.' }
    $pickupDate = (Get-Content (Join-Path $BaselineDir 'meta.json') -Raw | ConvertFrom-Json).pickupDate
}
else {
    # Next Monday at least 3 days out, so the date is valid for every carrier.
    $d = (Get-Date).Date.AddDays(3)
    while ($d.DayOfWeek -ne 'Monday') { $d = $d.AddDays(1) }
    $pickupDate = $d.ToString('yyyy-MM-dd')
}

[pscustomobject]@{ label = $Label; pickupDate = $pickupDate; capturedAt = (Get-Date).ToString('s') } |
    ConvertTo-Json | Set-Content (Join-Path $OutDir 'meta.json')

# Project a response to just the comparable facts: no ids, timings or correlation ids.
function Get-Comparable($response) {
    $calls = @($response.loggedCalls | ForEach-Object {
            [pscustomobject]@{
                ApiProvider  = $_.ApiProvider
                ShippingMode = $_.ShippingMode
                Operation    = $_.Operation
                PlantCode    = $_.PlantCode
                IsSuccess    = $_.IsSuccess
                IsTest       = $_.IsTest
                HttpStatus   = $_.HttpStatusCode
                Error        = $_.ErrorMessage
                Quotes       = @($_.Quotes | Sort-Object CarrierName, ServiceLevel, TotalCharge | ForEach-Object {
                        '{0}|{1}|base={2}|markup={3}|perPkg={4}|perShip={5}|total={6}|{7}|days={8}' -f `
                            $_.CarrierName, $_.ServiceLevel, $_.BaseCost, $_.MarkupPercent, $_.PerPackageCharge,
                        $_.PerShipmentCharge, $_.TotalCharge, $_.Currency, $_.TransitDays
                    })
            }
        } | Sort-Object ApiProvider, Operation, PlantCode)
    [pscustomobject]@{ processingError = $response.processingError; calls = $calls }
}

$failures = 0
foreach ($r in $requests) {
    $body = $r.body | ConvertTo-Json -Depth 6 | ConvertFrom-Json
    $body | Add-Member -NotePropertyName correlationId -NotePropertyValue "parity-$Label-$($r.name)" -Force
    $body | Add-Member -NotePropertyName pickupDate -NotePropertyValue $pickupDate -Force

    $outFile = Join-Path $OutDir "$($r.name).json"
    try {
        $resp = Invoke-RestMethod -Method Post -Uri $Endpoint -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 6)
    }
    catch {
        Write-Host "[$($r.name)] REQUEST FAILED: $($_.Exception.Message)" -ForegroundColor Red
        $failures++
        continue
    }
    $resp | ConvertTo-Json -Depth 10 | Set-Content $outFile

    # Every call must be flagged as a Test Call, in either mode.
    $notTest = @($resp.loggedCalls | Where-Object { -not $_.IsTest })
    if ($notTest.Count -gt 0) {
        Write-Host "[$($r.name)] FAIL: $($notTest.Count) logged call(s) not flagged IsTest" -ForegroundColor Red
        $failures++
    }

    if ($Mode -eq 'capture') {
        Write-Host "[$($r.name)] captured ($(@($resp.loggedCalls).Count) calls)"
        continue
    }

    $base = Get-Content (Join-Path $BaselineDir "$($r.name).json") -Raw | ConvertFrom-Json
    $a = (Get-Comparable $base | ConvertTo-Json -Depth 10) -split "`n"
    $b = (Get-Comparable $resp | ConvertTo-Json -Depth 10) -split "`n"
    $diff = Compare-Object $a $b
    if ($diff) {
        Write-Host "[$($r.name)] MISMATCH" -ForegroundColor Red
        $diff | ForEach-Object { Write-Host ("   {0} {1}" -f $_.SideIndicator, $_.InputObject.Trim()) }
        $failures++
    }
    else {
        Write-Host "[$($r.name)] match" -ForegroundColor Green
    }
}

# Surface where each provider's calls went, so a human can confirm LTL hit sandbox endpoints.
Get-ChildItem $OutDir -Filter *.json | Where-Object Name -ne 'meta.json' | ForEach-Object {
    (Get-Content $_.FullName -Raw | ConvertFrom-Json).loggedCalls
} | Where-Object { $_ } | ForEach-Object {
    $u = [Uri]$_.RequestUrl
    '{0,-22} {1,-8} {2}' -f $_.ApiProvider, $_.ShippingMode, $u.Host
} | Sort-Object -Unique | ForEach-Object { Write-Host "endpoint: $_" }

if ($failures -gt 0) { Write-Host "$failures problem(s)." -ForegroundColor Red; exit 1 }
Write-Host 'All requests OK.' -ForegroundColor Green

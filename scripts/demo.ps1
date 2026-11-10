<#
  Demostración del flujo "asignar un repartidor a una entrega".
  Requisitos: la API debe estar corriendo en otra terminal (dotnet run --project src/Api).
  Uso:  ./scripts/demo.ps1
#>
param(
    [string]$BaseUrl = "http://localhost:5080",
    [string]$ApiKey = "dev-dispatcher-key"
)

$Courier1 = "11111111-1111-1111-1111-111111111111"   # en línea, máximo 3
$Courier2 = "22222222-2222-2222-2222-222222222222"   # fuera de línea
$Courier3 = "33333333-3333-3333-3333-333333333333"   # en línea, máximo 1

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null, [bool]$UseKey = $true, [string]$Correlation = "demo")

    $headers = @{ "X-Correlation-Id" = $Correlation }
    if ($UseKey) { $headers["X-Api-Key"] = $ApiKey }

    $params = @{ Method = $Method; Uri = "$BaseUrl$Path"; Headers = $headers; ContentType = "application/json" }
    if ($null -ne $Body) { $params["Body"] = ($Body | ConvertTo-Json) }

    try {
        $data = Invoke-RestMethod @params
        return [pscustomobject]@{ Status = "OK"; Data = $data }
    }
    catch {
        $status = 0
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        return [pscustomobject]@{ Status = $status; Data = $_.ErrorDetails.Message }
    }
}

function Show-Step {
    param([string]$Title, $Result)
    Write-Host ""
    Write-Host "== $Title" -ForegroundColor Cyan
    if ($Result.Status -eq "OK") {
        Write-Host "OK (la API respondió con éxito)" -ForegroundColor Green
    }
    else {
        Write-Host ("Rechazada con HTTP " + $Result.Status) -ForegroundColor Yellow
    }
    if ($Result.Data -is [string]) { Write-Host $Result.Data } else { $Result.Data | ConvertTo-Json -Depth 5 }
}

function New-Delivery {
    param([string]$Correlation)
    $body = @{ orderId = [guid]::NewGuid().ToString(); address = "Calle 10 # 5-20, Barrio Centro" }
    return Invoke-Api -Method Post -Path "/api/v1/deliveries" -Body $body -Correlation $Correlation
}

# 1. Camino correcto
$a = New-Delivery -Correlation "demo-01"
Show-Step "1. Crear la entrega A" $a
$idA = $a.Data.deliveryId

Show-Step "2. Asignar el repartidor 1 a la entrega A" (Invoke-Api -Method Post -Path "/api/v1/deliveries/$idA/assign-courier" -Body @{ courierId = $Courier1 } -Correlation "demo-02")
Show-Step "3. Consultar la entrega A (debe estar Assigned)" (Invoke-Api -Method Get -Path "/api/v1/deliveries/$idA" -Correlation "demo-03")

# 2. Errores y límites
Show-Step "4. Asignar otra vez la entrega A (409: ya tiene repartidor)" (Invoke-Api -Method Post -Path "/api/v1/deliveries/$idA/assign-courier" -Body @{ courierId = $Courier1 } -Correlation "demo-04")

$b = New-Delivery -Correlation "demo-05"
Show-Step "5. Asignar a un repartidor fuera de línea (422)" (Invoke-Api -Method Post -Path "/api/v1/deliveries/$($b.Data.deliveryId)/assign-courier" -Body @{ courierId = $Courier2 } -Correlation "demo-05")

$c = New-Delivery -Correlation "demo-06"
$d = New-Delivery -Correlation "demo-07"
Show-Step "6a. Asignar C al repartidor 3 (cupo máximo 1: OK)" (Invoke-Api -Method Post -Path "/api/v1/deliveries/$($c.Data.deliveryId)/assign-courier" -Body @{ courierId = $Courier3 } -Correlation "demo-06")
Show-Step "6b. Asignar D al mismo repartidor (422: ruta saturada)" (Invoke-Api -Method Post -Path "/api/v1/deliveries/$($d.Data.deliveryId)/assign-courier" -Body @{ courierId = $Courier3 } -Correlation "demo-07")

Show-Step "7. Entrega inexistente (404)" (Invoke-Api -Method Post -Path "/api/v1/deliveries/$([guid]::NewGuid())/assign-courier" -Body @{ courierId = $Courier1 } -Correlation "demo-08")
Show-Step "8. Sin la clave de despachador (401)" (Invoke-Api -Method Get -Path "/api/v1/deliveries/$idA" -UseKey $false -Correlation "demo-09")

# 3. Mensajes fallidos (si la API se inició con Notifications__Mode=AlwaysFail)
Show-Step "9. Cola de mensajes fallidos" (Invoke-Api -Method Get -Path "/api/v1/admin/dead-letters" -Correlation "demo-10")

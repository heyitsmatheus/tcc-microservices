param(
    [string]$Scenario = "rest"  # rest | grpc | kafka
)

$containers = @{
    "rest"  = "order-processor-rest"
    "grpc"  = "order-processor-grpc"
    "kafka" = "order-processor-kafka"
}

$container = $containers[$Scenario]

Write-Host "=== Failure Scenario: $Scenario ===" -ForegroundColor Cyan
Write-Host "Container alvo: $container" -ForegroundColor Yellow
Write-Host ""
Write-Host "Aguardando 2 minutos antes de derrubar o Processor..." -ForegroundColor Green

# Aguarda 2 minutos com o sistema em carga normal
Start-Sleep -Seconds 120

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Derrubando $container..." -ForegroundColor Red
docker stop $container

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processor DERRUBADO. Aguardando 60 segundos..." -ForegroundColor Red
Start-Sleep -Seconds 60

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Reiniciando $container..." -ForegroundColor Green
docker start $container

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processor REINICIADO. Monitorando recuperacao..." -ForegroundColor Green
Start-Sleep -Seconds 60

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Cenario de falha concluido." -ForegroundColor Cyan
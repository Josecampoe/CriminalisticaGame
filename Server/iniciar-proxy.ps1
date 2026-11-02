# Arranca el proxy de IA de El Caso 404.
# Si no hay clave en el entorno, la pide en pantalla (oculta). La clave solo vive en esta ventana:
# no se escribe en ningún archivo. Deja esta ventana abierta mientras juegas.
Set-Location $PSScriptRoot

if (-not $env:ANTHROPIC_API_KEY) {
    $secure = Read-Host "Pega tu clave de API de Anthropic (empieza por sk-ant-; no se guarda)" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { $env:ANTHROPIC_API_KEY = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}

if (-not (Test-Path "node_modules")) {
    Write-Host "Instalando dependencias (solo la primera vez)..."
    npm install
}

node proxy.mjs

Add-Type -AssemblyName System.IO.Compression.FileSystem

$origen  = (Resolve-Path .\publish-inventario).Path
$destino = Join-Path (Get-Location) 'inventario.zip'
if (Test-Path $destino) { Remove-Item $destino }

$zip = [System.IO.Compression.ZipFile]::Open($destino, 'Create')
try {
    Get-ChildItem -Path $origen -Recurse -File | ForEach-Object {
        $rutaRelativa = $_.FullName.Substring($origen.Length + 1) -replace '\\', '/'
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $rutaRelativa)
    }
} finally {
    $zip.Dispose()
}
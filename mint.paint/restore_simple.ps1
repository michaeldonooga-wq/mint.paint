$lines = [System.IO.File]::ReadAllLines('DrawingMananger.cs', [System.Text.Encoding]::UTF8)
$newLines = @()
$skip = 0
for($i = 0; $i -lt $lines.Length; $i++) {
    if ($i -ge 173 -and $i -le 207) {
        if ($i -eq 173) {
            $newLines += '                        layerManager.DrawAllLayers(canvas);'
        }
        continue
    }
    $newLines += $lines[$i]
}
[System.IO.File]::WriteAllLines('DrawingMananger.cs', $newLines, [System.Text.Encoding]::UTF8)
Write-Host 'Restored simple version'

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$outputPath = "$($Env:MSBuildProjectDirectory)\bin\$($Env:Configuration)_$timestamp\"

$Env:OutputPath = $outputPath

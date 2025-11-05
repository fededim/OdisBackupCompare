dotnet publish /p:PublishProfile=Win-X64
dotnet publish /p:PublishProfile=Win-Arm64
dotnet publish /p:PublishProfile=MacOS-X64
dotnet publish /p:PublishProfile=MacOS-Arm64
dotnet publish /p:PublishProfile=Linux-X64
dotnet publish /p:PublishProfile=Linux-Arm64
$path = ".\OdisBackupCompare\bin\Release\net8.0\publish"
Get-ChildItem $path -Include *.pdb -Recurse | Remove-Item
copy .\OdisBackupCompare\SampleXML .\OdisBackupCompare\bin\Release\net8.0\publish -Recurse -Force
Compress-Archive -Path $path -DestinationPath "$path\release.zip" -CompressionLevel Optimal -Force

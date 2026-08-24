param(
    [ValidateSet("arm64", "x64", "all")]
    [string]$Arch = "arm64"
)

$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$AppProject = Join-Path $ProjectDir "src\HangUp.Mac.App\HangUp.Mac.App.csproj"
$DistDir = Join-Path $ProjectDir "dist"

function Build-AppBundle([string]$RuntimeId, [string]$OutputName) {
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host " Building HangUp for macOS ($RuntimeId)..." -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan

    $PublishDir = Join-Path $ProjectDir "src\HangUp.Mac.App\bin\Release\net8.0\$RuntimeId\publish"
    
    # 1. Publish Self-Contained Binary
    dotnet publish $AppProject -c Release -r $RuntimeId --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
    
    if (-not (Test-Path $PublishDir)) {
        Write-Error "Publish directory not found: $PublishDir"
    }

    # 2. Setup .app Directory Structure
    $AppBundle = Join-Path $DistDir "$OutputName.app"
    $ContentsDir = Join-Path $AppBundle "Contents"
    $MacOSDir = Join-Path $ContentsDir "MacOS"
    $ResourcesDir = Join-Path $ContentsDir "Resources"

    if (Test-Path $AppBundle) {
        Remove-Item $AppBundle -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path $MacOSDir | Out-Null
    New-Item -ItemType Directory -Force -Path $ResourcesDir | Out-Null

    # 3. Copy Publish files to MacOS directory
    Copy-Item "$PublishDir\*" -Destination $MacOSDir -Recurse -Force

    # 4. Copy Icon
    $IconSrc = Join-Path $ProjectDir "src\HangUp.Mac.App\Assets\handup.png"
    if (Test-Path $IconSrc) {
        Copy-Item $IconSrc -Destination (Join-Path $ResourcesDir "AppIcon.png") -Force
    }

    # 5. Create Info.plist
    $PlistContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>HangUp.Mac.App</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.hangup.firewall</string>
    <key>CFBundleName</key>
    <string>Hang Up !!</string>
    <key>CFBundleDisplayName</key>
    <string>Hang Up !!</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSRequiresAquaSystemAppearance</key>
    <false/>
</dict>
</plist>
"@
    Set-Content -Path (Join-Path $ContentsDir "Info.plist") -Value $PlistContent -Encoding UTF8

    # 6. Create PkgInfo
    Set-Content -Path (Join-Path $ContentsDir "PkgInfo") -Value "APPL????" -Encoding ASCII -NoNewline

    # 7. Create ZIP package with proper POSIX execute permissions (0755)
    $ZipFile = Join-Path $DistDir "$OutputName.zip"
    if (Test-Path $ZipFile) {
        Remove-Item $ZipFile -Force
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $zip = [System.IO.Compression.ZipFile]::Open($ZipFile, [System.IO.Compression.ZipArchiveMode]::Create)
    $files = Get-ChildItem -Path $AppBundle -Recurse -File

    foreach ($file in $files) {
        $relPath = $file.FullName.Substring($AppBundle.Length + 1).Replace('\', '/')
        $entryName = "$OutputName.app/$relPath"
        $entry = $zip.CreateEntry($entryName, [System.IO.Compression.CompressionLevel]::Optimal)

        # Set Unix file permissions (Upper 16 bits of ExternalAttributes)
        # 0x81ed0000 = -rwxr-xr-x (0100755 octal) for executables
        # 0x81a40000 = -rw-r--r-- (0100644 octal) for regular files
        if ($file.FullName -like "*Contents\MacOS\*" -or $file.Extension -in ".dylib", ".so") {
            $entry.ExternalAttributes = 0x81ed0000
        } else {
            $entry.ExternalAttributes = 0x81a40000
        }

        $fs = [System.IO.File]::OpenRead($file.FullName)
        $es = $entry.Open()
        $fs.CopyTo($es)
        $es.Dispose()
        $fs.Dispose()
    }
    $zip.Dispose()

    Write-Host " Build Complete!" -ForegroundColor Green
    Write-Host " App Bundle: $AppBundle" -ForegroundColor Yellow
    Write-Host " Zip File:   $ZipFile" -ForegroundColor Yellow
}

if ($Arch -eq "arm64" -or $Arch -eq "all") {
    Build-AppBundle -RuntimeId "osx-arm64" -OutputName "HangUp-AppleSilicon"
}

if ($Arch -eq "x64" -or $Arch -eq "all") {
    Build-AppBundle -RuntimeId "osx-x64" -OutputName "HangUp-Intel"
}

Write-Host "`nAll builds completed successfully! Check the 'dist' folder." -ForegroundColor Green

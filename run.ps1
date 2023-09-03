#! pwsh
<#
.SYNOPSIS
Helper for running various tasks

.DESCRIPTION
This script is a wrapper for snippets of dot net commands. It is useful to keep in the root of a project for running various tasks.

.PARAMETER Tasks
One or more tasks to run. Use tab completion to see available tasks.

.PARAMETER Version
For pack task, the version to use. e.g. 1.0.0

.PARAMETER Prerelease
For pack task, if specified, will add --version-suffix prerelease to pack command

.PARAMETER appName
The name of the app folder. Defaults to OptionToStringGenerator

.PARAMETER BuildLogFolder
The folder to write build logs to. Defaults to the system temp folder

.EXAMPLE
./run.ps1 build

#>
[CmdletBinding()]
param (
    [ArgumentCompleter({
        param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
        $runFile = (Join-Path (Split-Path $commandAst -Parent) run.ps1)
        if (Test-Path $runFile) {
            Get-Content $runFile |
                    Where-Object { $_ -match "^\s+'([\w+-]+)'\s*{" } |
                    ForEach-Object {
                        if ( !($fakeBoundParameters[$parameterName]) -or
                            (($matches[1] -notin $fakeBoundParameters.$parameterName) -and
                             ($matches[1] -like "$wordToComplete*"))
                            )
                        {
                            $matches[1]
                        }
                    }
        }
     })]
    [string[]] $Tasks,
    [string] $Version,
    [switch] $Prerelease,
    [string] $appName = "OptionToStringGenerator",
    [string] $BuildLogFolder
)

$currentTask = ""
if (!$BuildLogFolder) {
    $BuildLogFolder = [System.IO.Path]::GetTempPath()
}

# execute a script, checking lastexit code
function executeSB
{
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [scriptblock] $ScriptBlock,
    [string] $RelativeDir,
    [string] $Name = $currentTask
)
    if ($RelativeDir) {
        Push-Location (Join-Path $PSScriptRoot $RelativeDir)
    } else {
        Push-Location $PSScriptRoot
    }
    try {
        $global:LASTEXITCODE = 0

        Invoke-Command -ScriptBlock $ScriptBlock

        if ($LASTEXITCODE -ne 0) {
            throw "Error executing command '$Name', last exit $LASTEXITCODE"
        }
    } finally {
        Pop-Location
    }
}

foreach ($currentTask in $Tasks) {

    try {
        $prevPref = $ErrorActionPreference
        $ErrorActionPreference = "Stop"

        "-------------------------------"
        "Starting $currentTask"
        "-------------------------------"

        switch ($currentTask) {
            'build' {
                executeSB -RelativeDir "src/$appName" {
                    dotnet build
                }
            }
            'testUnit' {
                executeSB -RelativeDir "src/tests/unit" {
                    dotnet test --collect:"XPlat Code Coverage"
                }
            }
            'testIntegration' {
                executeSB -RelativeDir "src/tests/integration" {
                    dotnet test --collect:"XPlat Code Coverage"
                }
            }
            'run' {
                executeSB -RelativeDir "src/${appName}" {
                    dotnet run
                }
            }
            'watch' {
                executeSB -RelativeDir "src/${appName}" {
                    dotnet watch
                }
            }
            'createLocalNuget' {
                executeSB -Name 'CreateNuget' {
                    $localNuget = dotnet nuget list source | Select-String "Local \[Enabled" -Context 0,1
                    if (!$localNuget) {
                        if (!$LocalNugetFolder) {
                            $LocalNugetFolder = (Join-Path $PSScriptRoot 'packages')
                            $null = New-Item 'packages' -ItemType Directory -ErrorAction Ignore
                        }
                        dotnet nuget add source $LocalNugetFolder --name Local
                    }
                    }
            }
            'pack' {
                if ($Version) {
                    "Packing with version $Version"
                    $localNuget = dotnet nuget list source | Select-String "Local \[Enabled" -Context 0,1
                    if ($localNuget) {
                        $AppName | ForEach-Object {
                            $params = @()
                            if ($Prerelease) {
                                $params += '--version-suffix'
                                $params += 'prerelease'
                                $params += '--include-source'
                                $params += '--include-symbols'
                            }
                            executeSB -RelativeDir "src/$_" -Name "$currentTask $_" {
                                $logFile = Join-Path $BuildLogFolder Build.log

                                # pack directly to local nuget folder since may not be able to push
                                dotnet pack -o ($localNuget.Context.PostContext.Trim()) `
                                            -p:VersionPrefix=$Version `
                                            -p:AssemblyVersion=$Version `
                                            /warnaserror `
                                            "/flp:logfile=`"$logFile`";Append" `
                                            @params
                                Write-Information "Packed. Logfile is $logFile" -InformationAction Continue
                            }
                        }
                    } else {
                        throw "Must have a 'Local' NuGet source for testing. e.g. dotnet nuget sources add -name Local -source c:\nupkgs"
                    }
                } else {
                    throw "Must supply Version for pack"
                }
            } Default {
                Write-Warning "Unknown task $currentTask"
            }
        }

    } finally {
        $ErrorActionPreference = $prevPref
    }
}

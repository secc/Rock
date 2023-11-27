class Project {
    [Guid]$Id
    [string]$Name
    [Guid]$Type
    [string]$ProjectFile
    [Guid[]]$References
}

Function Get-NuGet-References {
    param (
        [string]$ProjectFile
    )

    [XML]$csproj = Get-Content $ProjectFile

    $Refs = @()
    foreach ($reference in $csproj.Project.ItemGroup.Reference) {
        if ($reference.HintPath) {
            $Refs += $reference.HintPath
        }
    }

    return $Refs
}

Function Read-Solution-File {
    param (
        [string]$SolutionFile
    )

    $sln = Get-Content $SolutionFile
    $Projects = @()

    for ($LineNumber = 0; $LineNumber -lt $sln.Count; $LineNumber++) {
        # Check for start of project line.
        $ProjectMatch = [RegEx]::Match($sln[$LineNumber], "^Project\(""{([^""]*)}""\)\s*=\s*""([^""]*)"",\s*""([^""]*)"",\s*""{([^""]*)}""")
        if ($ProjectMatch.Success) {
            $Project = [Project]@{
                Id = [Guid]::Parse($ProjectMatch.Groups[4].Value)
                Name = $ProjectMatch.Groups[2].Value
                Type = [Guid]::Parse($ProjectMatch.Groups[1].Value)
                ProjectFile = $ProjectMatch.Groups[3].Value
                References = @()
            }

            for (; $LineNumber -lt $sln.Count; $LineNumber++) {
                # Check if there are any project references defined.
                $ReferenceMatch = [RegEx]::Match($sln[$LineNumber], "ProjectReferences\s*=\s*""([^""]+)""")
                if ($ReferenceMatch.Success) {
                    $Refs = $ReferenceMatch.Groups[1].Value.Split(";")
                    ForEach ($Ref in $Refs) {
                        If ($Ref.Length -gt 0) {
                            $Project.References += [Guid]::Parse($Ref.Split("|")[0])
                        }
                    }
                }

                # Check for end of project marker.
                if ($sln[$LineNumber].StartsWith("EndProject")) {
                    if ($Project['Type'] -eq "2150E333-8FDC-42A3-9474-1A3956D46DE8") {
                        break
                    }

                    $Projects += $Project

                    break;
                }
            }
        }
    }

    return $Projects
}

Function Copy-NuGet-References {
    param (
        [string]$SolutionPath,
        [Project]$WebSiteProject,
        [Project[]]$Projects
    )

    $BinPath = Join-Path $SolutionPath $WebSiteProject.ProjectFile
    $BinPath = Join-Path $BinPath "Bin"
    $BinPath = Resolve-Path $BinPath

    ForEach ($RefId in $WebSiteProject.References) {
        $ReferencedProject = $Projects | Where-Object -Property Id -EQ -Value $RefId
        if (!$ReferencedProject) {
            throw "Project $($RefId) was found in the solution file."
        }

        $ReferencedProjectFile = Join-Path $SolutionPath $ReferencedProject.ProjectFile
        $ProjectPath = Split-Path -Parent $ReferencedProjectFile
        $ReferencedFiles = Get-NuGet-References $ReferencedProjectFile

        ForEach ($File in $ReferencedFiles) {
            $SourceDirectory = Join-Path $ProjectPath $File | Resolve-Path | Split-Path -Parent

            # Skip files that were referenced from the website folder.
            if ($SourceDirectory -eq $BinPath) {
                continue
            }

            $Filenames = @("*.dll", "*.pdb", "*.xml") | ForEach-Object { Get-ChildItem $SourceDirectory -File -Filter $_ }
            ForEach ($Filename in $Filenames) {
                $FilenameOnly = $Filename.Name

                # This file is not copied in Visual Studio.
                If ($FilenameOnly -Eq "System.Management.Automation.dll") {
                    continue
                }

                If ($FilenameOnly -Eq "system.web.optimization.xml") {
                    $FilenameOnly = "System.Web.Optimization.xml"
                }

                $SourceFile = Join-Path $SourceDirectory $FilenameOnly.Replace(".XML", ".xml")
                $SourceFilename = Split-Path -Leaf $SourceFile

                # Skip reference and facade files since they are automatic.
                $ReferenceFile = Join-Path "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2" $SourceFilename
                $FacadeFile = Join-Path "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\Facades" $SourceFilename
                if ($(Test-Path $ReferenceFile) -Or $(Test-Path $FacadeFile)) {
                    continue
                }

                Copy-Item $SourceFile -Destination $BinPath
            }
        }
    }
}

# Force the script to error out on any error
$ErrorActionPreference = "Stop"

$SolutionFile = Resolve-Path $args[0]
$WebSiteName = "RockWeb"
$SolutionPath = Split-Path -Parent $SolutionFile

# Parse the solution file.
$Projects = Read-Solution-File $SolutionFile

# Find the website project.
$WebSiteProject = $Projects | Where-Object -Property "Name" -EQ -Value $WebSiteName
if (!$WebSiteProject) {
    throw "WebSite $($WebSiteName) was not found in solution file."
}

# Find all references and copy all nuget DLLs into the site.
Copy-NuGet-References $SolutionPath $WebSiteProject $Projects

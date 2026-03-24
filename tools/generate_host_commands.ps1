$xmlDir = "ThalesCore\XMLDefs\HostCommands"
$outDir = "ThalesCore\HostCommands\BuildIn"

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

Get-ChildItem -Path $xmlDir -Filter *.xml | ForEach-Object {
    $xmlPath = $_.FullName
    try {
        $xml = [xml](Get-Content $xmlPath -Raw)
    } catch {
        Write-Host "Failed to parse $xmlPath`n$_"
        return
    }
    $request = $xml.CommandConfiguration.Request
    $response = $xml.CommandConfiguration.Response
    $commandName = $xml.CommandConfiguration.CommandName -replace '"','\"'
    $className = [System.IO.Path]::GetFileNameWithoutExtension($xmlPath)
    $outPath = Join-Path $outDir ($className + ".cs")
    if (Test-Path $outPath) { Write-Host "Skipping existing: $outPath"; return }

    $code = @"
using HostCommands;
using System;
using ThalesCore;
using ThalesCore.Message.XML;
using ThalesCore.HostCommands;
using ThalesCore.Message;

namespace ThalesCore.HostCommands.BuildIn
{
    [ThalesCommandCode("$request", "$response", "", "$commandName")]
    public class $className : AHostCommand
    {
        public $className()
        {
            ReadXMLDefinitions();
        }

        public override void AcceptMessage(ThalesCore.Message.Message msg)
        {
            string ret = string.Empty;
            ThalesCore.Message.XML.MessageParser.Parse(msg, XMLMessageFields, ref kvp, out ret);
            XMLParseResult = ret;
        }

        public override MessageResponse ConstructResponse()
        {
            MessageResponse mr = new MessageResponse();
            mr.AddElement(ErrorCodes.ER_00_NO_ERROR);
            return mr;
        }
    }
}
"@

    Set-Content -Path $outPath -Value $code -Encoding UTF8
    Write-Host "Created: $outPath"
}

Write-Host "Generation complete."

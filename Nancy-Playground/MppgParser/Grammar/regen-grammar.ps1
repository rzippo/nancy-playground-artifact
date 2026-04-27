#!/usr/bin/pwsh

# This script regenerates the ANTLR4 parser code from the grammar file Mppg.g4
# It first checks if a compatible version of ANTLR4 is installed globally,
# otherwise it downloads the ANTLR4 jar file and uses that.

Push-Location $PSScriptRoot
try {
    # This variable will contain the antlr command to call
    $antlrCommand = $null;

    # First check if a compatible version is globally installed
    # 'Compatible' currently means 4.13.x
    if(Get-Command antlr4 -ErrorAction SilentlyContinue)
    {
        $output = @( antlr4 );
        $isMatch = $output[0] -match "Version (\d+\.\d+)\.\d+";
        if($isMatch)
        {
            $version = $Matches[1];
            if($version -eq "4.13")
            {
                $antlrCommand = "antlr4";
            }
        }
    }

    # Else, we download and use the jar version locally
    if($null -eq $antlrCommand)
    {
        $filename = "antlr/antlr-4.13.2-complete.jar";
        $url = "https://www.antlr.org/download/antlr-4.13.2-complete.jar";
        if(Test-Path $filename)
        {
            # The jar was already downloaded
        }
        else
        {
            if(-not(Test-Path "antlr")) {
                New-Item -ItemType Directory "antlr"
            }
            Invoke-WebRequest -Uri $url -OutFile $filename;
        }
        $antlrCommand = "java -jar $filename";
    }

    $command = "$antlrCommand -Dlanguage=CSharp -o ./ -package Unipi.MppgParser.Grammar -visitor -no-listener -encoding utf-8 -lib ./ ./Mppg.g4";
    Invoke-Expression $command;
}
finally {
    Pop-Location
}

#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"
#load "Versions.fsx"
#load "BuildConfig.fsx"

open System
open System.Diagnostics
open System.Text
open System.IO
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Fake
open Fake.AssemblyInfoFile
open Fake.FileHelper
open Fake.Git
open Fake.Testing.XUnit2
open Products.Products
open Products.Paths
open Versions

module Builder =
    open Versions.Versions

    let Sign file (version : Versions.Version) =
        let release = getBuildParam "release" = "1"
        if release then
            let certificate = getBuildParam "certificate"
            let password = getBuildParam "password"
            let timestampServer = "http://timestamp.comodoca.com"
            let timeout = TimeSpan.FromMinutes 1.

            let sign () =
                let signToolExe = ToolsDir @@ "signtool/signtool.exe"
                let args = ["sign"; "/f"; certificate; "/p"; password; "/t"; timestampServer; "/d"; version.Product.Title; "/v"; file] |> String.concat " "
                let redactedArgs = args.Replace(password, "<redacted>")

                use proc = new Process()
                proc.StartInfo.UseShellExecute <- false
                proc.StartInfo.FileName <- signToolExe
                proc.StartInfo.Arguments <- args
                platformInfoAction proc.StartInfo
                proc.StartInfo.RedirectStandardOutput <- true
                proc.StartInfo.RedirectStandardError <- true
                if isMono then
                    proc.StartInfo.StandardOutputEncoding <- Encoding.UTF8
                    proc.StartInfo.StandardErrorEncoding  <- Encoding.UTF8
                proc.ErrorDataReceived.Add(fun d -> if d.Data <> null then traceError d.Data)
                proc.OutputDataReceived.Add(fun d -> if d.Data <> null then trace d.Data)

                try
                    tracefn "%s %s" proc.StartInfo.FileName redactedArgs
                    start proc
                with exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message
                proc.BeginErrorReadLine()
                proc.BeginOutputReadLine()
                if not <| proc.WaitForExit(int timeout.TotalMilliseconds) then
                    try
                        proc.Kill()
                    with exn ->
                        traceError
                        <| sprintf "Could not kill process %s  %s after timeout." proc.StartInfo.FileName redactedArgs
                    failwithf "Process %s %s timed out." proc.StartInfo.FileName redactedArgs
                proc.WaitForExit()
                proc.ExitCode

            let exitCode = sign()
            if exitCode <> 0 then failwithf "Signing %s returned error exit code: %i" version.Product.Title exitCode
    
    let patchAssemblyInformation (version:Version) = 
        let commitHash = Information.getCurrentHash()
        let file = version.ServiceDir() @@ "Properties" @@ "AssemblyInfo.cs"
        CreateCSharpAssemblyInfo file
            [Attribute.Title version.Product.AssemblyTitle
             Attribute.Description version.Product.AssemblyDescription
             Attribute.Guid version.Product.AssemblyGuid
             Attribute.Product version.Product.Title
             Attribute.Metadata("GitBuildHash", commitHash)
             Attribute.Company  "Elasticsearch BV"
             Attribute.Copyright "Apache License, version 2 (ALv2). Copyright Elasticsearch."
             Attribute.Trademark (sprintf "%s is a trademark of Elasticsearch BV, registered in the U.S. and in other countries." version.Product.Title)
             Attribute.Version version.FullVersion
             Attribute.FileVersion version.FullVersion
             Attribute.InformationalVersion version.FullVersion // Attribute.Version and Attribute.FileVersion normalize the version number, so retain the prelease suffix
            ]
    
    let BuildService (version:Version) = 

            patchAssemblyInformation version 
        
            !! (version.ServiceDir() @@ "*.csproj")
            |> MSBuildRelease (version.ServiceBinDir()) "Build"
            |> ignore
        
            let serviceAssembly = (version.ServiceBinDir()) @@ (sprintf "%s.exe" version.Product.Name)
            let service = binDir @@ (sprintf "%s.exe" product.Name)
            CopyFile service serviceAssembly
            Sign service product

    let BuildMsi (version:Version, compile:bool) =

        let filePath = version.OutMsiPath()
        if (compile) then
            !! (MsiDir @@ "*.csproj")
            |> MSBuildRelease MsiBuildDir "Build"
            |> ignore
            let exitCode = ExecProcess (fun info ->
                             info.FileName <- sprintf "%sElastic.Installer.Msi" MsiBuildDir
                             info.WorkingDirectory <- MsiDir
                             info.Arguments <- [version.Product.Name; version.FullVersion; Path.GetFullPath(InDir)] |> String.concat " "
                            ) <| TimeSpan.FromMinutes 20.
    
            if exitCode <> 0 then failwithf "Error building MSI for %s" version.Product.Name
            CopyFile filePath (MsiDir @@ (sprintf "%s.msi" version.Product.Name))
            Sign filePath version
        else
            if not <| fileExists (version.DownloadPath()) then failwithf "No file found at %s" (product.DownloadPath version)
            CopyFile (filePath product version) (version.Product.DownloadPath)
#I "../../packages/build/FAKE/tools"

#r "FakeLib.dll"

open System
open System.Globalization
open System.Text
open System.IO
open System.Text.RegularExpressions
open Fake
open Fake.FileHelper

module Paths =

    let BuildDir = "./build/"
    let ToolsDir = BuildDir @@ "tools/"
    let InDir = BuildDir @@ "in/"
    let OutDir = BuildDir @@ "out/"
    let ResultsDir = BuildDir @@ "results/"

    let SrcDir = "./src/"
    let ProcessHostsDir = SrcDir @@ "ProcessHosts/"
    let MsiDir = SrcDir @@ "Installer/Elastic.Installer.Msi/"
    let MsiBuildDir = MsiDir @@ "bin/Release/"

    let IntegrationTestsDir = FullName "./src/Tests/Elastic.Installer.Integration.Tests"
    let UnitTestsDir = "src/Tests/Elastic.Domain.Tests"

    let ArtifactDownloadsUrl = "https://artifacts.elastic.co/downloads"

module Products =
    open Paths
    
    type Extension = 
        Plugin of string
        | Pack of string

    type Product =
        | Elasticsearch
        | Kibana

        member this.Name =
            match this with
            | Elasticsearch -> "elasticsearch"
            | Kibana -> "kibana"
            
        member this.AssemblyTitle =
            match this with
            | Elasticsearch -> "Elasticsearch, you know for search!"
            | Kibana -> "kibana"
            
        member this.AssemblyDescription =
            match this with
            | Elasticsearch -> "Elasticsearch is a distributed, RESTful search and analytics engine capable of solving a growing number of use cases. As the heart of the Elastic Stack, it centrally stores your data so you can discover the expected and uncover the unexpected."
            | Kibana -> "kibana"
            
        member this.AssemblyGuid =
            match this with
            | Elasticsearch -> "d4fb307f-cb1d-4026-bd28-ca1d0016d709"
            | Kibana -> "ffb9da32-12fa-4c9d-a5bd-06cddae74fd4"
            
            
        member this.Extensions =
            match this with
            | Elasticsearch -> 
            [
                Pack "x-pack";
                Plugin "ingest-attachment";
                Plugin "ingest-geoip";
                Plugin "analysis-icu";
                Plugin "analysis-kuromoji";
                Plugin "analysis-phonetic";
                Plugin "analysis-smartcn";
                Plugin "discovery-ec2";
                Plugin "discovery-azure-classic";
                Plugin "discovery-gce";
                Plugin "mapper-size";
                Plugin "mapper-murmur3";
                Plugin "lang-javascript";
                Plugin "lang-python";
                Plugin "repository-hdfs";
                Plugin "repository-s3";
                Plugin "repository-azure";
                Plugin "store-smb";
            ]
            | Kibana -> List.empty
            

        member this.Title =
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase this.Name

    let All = [Elasticsearch; Kibana]

    type Version = {
        Product : string;
        FullVersion : string;
        Major : int;
        Minor : int;
        Patch : int;
        Prerelease : string;
    }

    type ProductVersions(product:Product, versions:Version list) =
        member this.Product = product
        member this.Versions = versions
        member this.Name = product.Name
        member this.Title = product.Title

        member this.DownloadUrls =
            this.Versions
            |> List.map(fun v ->
                match product with
                | Elasticsearch ->
                    sprintf "%s/elasticsearch/elasticsearch-%s.zip" ArtifactDownloadsUrl v.FullVersion
                | Kibana ->               
                    sprintf "%s/kibana/kibana-%s-windows-x86.zip" ArtifactDownloadsUrl v.FullVersion                       
            )
        
        member this.ZipFiles =
            InDir |> CreateDir
            let fullPathInDir = InDir |> Path.GetFullPath
            this.Versions
            |> List.map(fun v ->
                Path.Combine(fullPathInDir, sprintf "%s-%s.zip" this.Name v.FullVersion)
            )

        member this.ExtractedDirectories =
            InDir |> CreateDir
            let fullPathInDir = InDir |> Path.GetFullPath            
            this.Versions
            |> List.map (fun v ->
                Path.Combine(fullPathInDir, sprintf "%s-%s" this.Name v.FullVersion)
            )

        member this.BinDirs = 
            this.Versions
            |> List.map(fun v -> InDir @@ sprintf "%s-%s/bin/" this.Name v.FullVersion)

        member this.ServiceDir =
            ProcessHostsDir @@ sprintf "Elastic.ProcessHosts.%s/" this.Title

        member this.ServiceBinDir = this.ServiceDir @@ "bin/AnyCPU/Release/"

        member this.Unzip () =
            List.zip3 this.ExtractedDirectories this.ZipFiles this.Versions
            |> List.iter(fun (extractedDirectory, zipFile, version) ->
                if directoryExists extractedDirectory |> not && fileExists zipFile
                then
                    tracefn "Unzipping %s %s" this.Name zipFile
                    Unzip InDir zipFile
                    match this.Product with
                        | Kibana ->
                            let original = sprintf "kibana-%s-windows-x86" version.FullVersion
                            if directoryExists original |> not then
                                Rename (InDir @@ (sprintf "kibana-%s" version.FullVersion)) (InDir @@ original)
                        | _ -> ()
                else tracefn "Extracted directory %s already exists" extractedDirectory                
            )
            
    //https://artifacts.elastic.co/downloads/packs/x-pack/x-pack-5.4.1.zip
    //https://artifacts.elastic.co/downloads/packs/x-pack/x-pack-5.4.1.zip
    //https://artifacts.elastic.co/downloads/elasticsearch-plugins/ingest-attachment/ingest-attachment-6.0.0-alpha1.zip
        member this.DownloadExtensions () = 
            let extensionZip v e =  
                match e with 
                | Pack x -> sprintf "%s-%s.zip" x v.FullVersion
                | Plugin x -> sprintf "%s-%s.zip" x v.FullVersion
            let extensionUrl v e =  
                match e with 
                | Pack x -> sprintf "%s/packs/%s/%s" ArtifactDownloadsUrl x (extensionZip v e)
                | Plugin x -> sprintf "%s/%s-plugins/%s/%s" ArtifactDownloadsUrl this.Name x (extensionZip v e)
            let fullPathInDir = InDir |> Path.GetFullPath
            let downloadUrls = 
                this.Versions
                |> List.collect (fun v -> 
                   product.Extensions
                   |> List.map(fun e -> 
                       let url = extensionUrl v e
                       let zip = Path.Combine(fullPathInDir, extensionZip v e)
                       (url, zip)
                    )
                )
            downloadUrls
            |> List.iter(fun location ->          
                match location with
                | (_, zip) when fileExists zip -> tracefn "Already downloaded %s to %s" this.Name zip
                | (url, zip) ->
                    tracefn "Downloading %s extension: %s" this.Name url 
                    use webClient = new System.Net.WebClient()
                    location |> webClient.DownloadFile
                    tracefn "Done downloading %s extension to %s" this.Name zip 
            )  
            

        member this.Download () =
            let locations = List.zip this.DownloadUrls this.ZipFiles
            locations
            |> List.iter(fun location ->          
                match location with
                | (_, zip) when fileExists zip ->
                    tracefn "Already downloaded %s to %s" this.Name zip
                | (url, zip) ->
                    tracefn "Downloading %s from %s" this.Name url 
                    use webClient = new System.Net.WebClient()
                    location |> webClient.DownloadFile
                    tracefn "Done downloading %s from %s to %s" this.Name url zip 
            )  

        static member CreateFromProduct (productToVersion:Product -> Version list) (product: Product)  =
            ProductVersions(product, productToVersion product)



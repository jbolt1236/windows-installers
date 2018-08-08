#I "../../packages/build/FAKE.x64/tools"
#I "../../packages/build/Fsharp.Data/lib/net40"
#I "../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r "FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"

#load "Products.fsx"

open System
open System.Text.RegularExpressions
open FSharp.Data
open FSharp.Text.RegexProvider
open Microsoft.FSharp.Reflection
open Products
open Paths

module Versions =

    type Distribution =
        | MSI
        | Zip

    type Source =
        | Official // Official releases
        | Staging  // Build candidates for official release
        | Snapshot // On-demand and nightly builds

    type RequestedVersionPart =
        | Number of int
        | Latest
        member this.Display = match this with
                              | Number number -> sprintf "%i" number
                              | Latest -> "*"

    type RequestedPrereleasePart =
        | Prerelease of string
        | Stable
        | Any
        member this.Display = match this with
                              | Prerelease prerelease -> sprintf "-%s" prerelease
                              | Stable -> ""
                              | Any -> "-*"

    type RequestedVersion =
        | Version of major:RequestedVersionPart * minor:RequestedVersionPart * patch:RequestedVersionPart * prerelease:RequestedPrereleasePart
        | Hash of string
        static member Latest = Version (Latest, Latest, Latest, Stable)
        member this.Display = match this with
                              | Version (major, minor, patch, prerelease) -> sprintf "%s.%s.%s%s" (major.Display) (minor.Display) (patch.Display) (prerelease.Display)
                              | Hash hash -> sprintf "%s" hash

    type RequestedAsset =
        struct
            val Product : Products.Product;
            val Version : RequestedVersion;
            val Distribution : Distribution;
            val Source : Source;
            new (product, version, distribution, source) = { Product = product; Version = version; Distribution = distribution; Source = source }
        end
        static member Latest = new RequestedAsset (Products.Elasticsearch, RequestedVersion.Latest, Zip, Official)

    type ResolvedVersion = {
        RequestedAsset : RequestedAsset;
        Product : Products.Product;
        FullVersion : string;
        Major : int;
        Minor : int;
        Patch : int;
        Prerelease : string;
        Hash : string;
    }

    let (|DistributionMatch|) (candidate:string) =
        match candidate with
        | "msi" -> Some MSI
        | "zip" -> Some Zip
        | _ -> None

    let (|SourceMatch|) (candidate:string) =
        match candidate with
        | "official" -> Some Official
        | "staging" -> Some Staging
        | "snapshot" -> Some Snapshot
        | _ -> None

    let (|ProductMatch|) (candidate:string) =
        match candidate with
        | "e"
        | "es"
        | "elasticsearch" -> Some Products.Elasticsearch
        | "k"
        | "kibana" -> Some Products.Kibana
        | _ -> None

    let (|Int|_|) str =
       match System.Int32.TryParse(str) with
       | (true,int) -> Some int
       | _ -> None

    let (|RequestedVersionPartMatch|) (candidate:string) =
        match candidate with
        | Int number -> Some (Number number)
        | "*" -> Some Latest
        | _ -> None

    let (|HashMatch|_|) (candidate:string) =
        if candidate = null || candidate = "" then
            None
        else
            let m = Regex.IsMatch(candidate, "^[0-9a-f]{8,8}$")
            if m then Some candidate
            else None

    let PrereleasePart (candidate:string) =
        if candidate.IndexOf('-') > 0 then
            let split = candidate.Split('-')
            let last = Seq.last split
            if last = "*" then
                Any
            else
                Prerelease (last)
        else
           Stable

    let (|RequestedVersionMatch|) (candidate:string) =
        if candidate = null || candidate = "" then
            Some RequestedVersion.Latest
        else
            let prereleasePart = PrereleasePart candidate
            let numberedPart = candidate.Split('-') |> Seq.head
            match numberedPart.Split('.') with
            | [|RequestedVersionPartMatch major; RequestedVersionPartMatch minor; RequestedVersionPartMatch patch;|]
                when (major <> Some Latest && minor <> Some Latest) || (minor = Some Latest && patch = Some Latest)
                -> Some (Version (major.Value, minor.Value, patch.Value, prereleasePart))
            | [|RequestedVersionPartMatch major; RequestedVersionPartMatch minor;|]
                when major <> Some Latest
                -> Some (Version (major.Value, minor.Value, Latest, prereleasePart))
            | [|RequestedVersionPartMatch major;|]
                -> Some (Version (major.Value, Latest, Latest, prereleasePart))
            | [|RequestedVersionPartMatch major;|]
                -> Some (Version (major.Value, Latest, Latest, prereleasePart))
            | _ -> None

    let (|RequestedAssetMatch|) (candidate:string) =
        if candidate = null || candidate = "" then
            Some RequestedAsset.Latest
        else
            match candidate.Split(':') with
            | [|ProductMatch product; HashMatch hash|]
                -> Some (new RequestedAsset (product.Value, Hash hash, Zip, Staging))
            | [|ProductMatch product; RequestedVersionMatch version; DistributionMatch distribution; SourceMatch source|]
                -> Some (new RequestedAsset (product.Value, version.Value, distribution.Value, source.Value))
            | [|ProductMatch product; RequestedVersionMatch version; DistributionMatch distribution|]
                -> Some (new RequestedAsset (product.Value, version.Value, distribution.Value, Official))
            | [|ProductMatch product; HashMatch hash |]
                -> Some (new RequestedAsset (product.Value, Hash hash, Zip, Staging))
            | [|ProductMatch product; RequestedVersionMatch version;|]
                -> Some (new RequestedAsset (product.Value, version.Value, Zip, Official))
            | [|ProductMatch product; |]
                -> Some (new RequestedAsset (product.Value, RequestedVersion.Latest, Zip, Official))
            | _ -> None

    [<Literal>]
    let private feedUrl = "https://www.elastic.co/downloads/past-releases/feed"

    [<Literal>]
    let private feedExample = "feed-example.xml"
    
    type DownloadFeed = XmlProvider< feedExample >

    type VersionRegex = Regex< @"^(?:\s*(?<Product>.*?)\s*)?((?<Source>\w*)\:)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)", noMethodPrefix=true >

    let fromString<'a> (s:string) =
        match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
        |[|case|] -> Some(FSharpValue.MakeUnion(case,[||]) :?> 'a)
        |_ -> None

    let parseVersion (requested: RequestedAsset, version:string, hash:string) =
        let m = VersionRegex().Match version
        if m.Success |> not then failwithf "Could not parse version from %s" version
        { RequestedAsset = requested;
          FullVersion = m.Version.Value;
          Product = (fromString<Products.Product> m.Product.Value).Value;
          Major = m.Major.Value |> int;
          Minor = m.Minor.Value |> int;
          Patch = m.Patch.Value |> int;
          Prerelease = m.Prerelease.Value;
          Hash = hash }
          
    let private findInOfficialFeed (requested : RequestedAsset) =
        let feed = DownloadFeed.Load feedUrl
        let allVersions = feed.Channel.Items
                          |> Seq.filter (fun item -> item.Title.StartsWith(requested.Product.Title))
                          |> Seq.map (fun item -> parseVersion (requested, item.Title, ""))                   
                          |> Seq.filter (fun version -> version.Product = requested.Product )
        match requested.Version with
        | Hash _ -> None // Official releases do not contain hashes
        | Version (Latest, _, _, Stable)
            -> Some (Seq.head(allVersions))
        | Version (Number major, Latest, _, Stable)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major)
        | Version (Number major, Number minor, Latest, Stable)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor)
        | Version (Number major, Number minor, Number patch, Stable)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor && item.Patch = patch)
        | _ -> None

    let getStagingVersions = (
        use webClient = new System.Net.WebClient()
        let url = "https://artifacts-api.elastic.co/v1/versions"
        let versions = webClient.DownloadString url |> JsonValue.Parse
        let arrayValue = versions.GetProperty "versions"
        arrayValue.AsArray()
        |> Seq.rev
        |> Seq.map (fun x -> x.AsString())
     )

    let getStagingBuilds version = (
       use webClient = new System.Net.WebClient()
       let url = "https://artifacts-api.elastic.co/v1/versions/" + version + "/builds"
       let versions = webClient.DownloadString url |> JsonValue.Parse
       let arrayValue = versions.GetProperty "builds"
       arrayValue.AsArray()
       |> Seq.rev
       |> Seq.map (fun x -> x.AsString())
       |> Seq.map (fun s -> (Seq.last( s.Split '-'), s.Replace("-" + Seq.last( s.Split '-'), "")))
    )

    let findInStagingFeed (requested : RequestedAsset) =
        let allVersions = getStagingVersions
                          |> Seq.map (fun x -> getStagingBuilds x)
                          |> Seq.concat
                          |> Seq.map (fun item -> parseVersion (requested, snd item, fst item))
        match requested.Version with
        | Hash hash
            -> allVersions |> Seq.tryFind (fun item -> item.Hash = hash)
        | Version (Latest, _, _, Any)
            -> Some (Seq.head(allVersions))        
        | Version (Number major, Latest, _, Any)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major)        
        | Version (Number major, Number minor, Latest, Any)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor)        
        | Version (Number major, Number minor, Number patch, Any)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor && item.Patch = patch)
        | Version (Latest, _, _, Stable)
            -> allVersions |> Seq.tryFind (fun item -> item.Prerelease = "")        
        | Version (Number major, Latest, _, Stable)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Prerelease = "")        
        | Version (Number major, Number minor, Latest, Stable)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor && item.Prerelease = "")        
        | Version (Number major, Number minor, Number patch, Stable)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor && item.Patch = patch && item.Prerelease = "")
        | Version (Latest, _, _, Prerelease prerelease)
            -> allVersions |> Seq.tryFind (fun item -> item.Prerelease = prerelease)        
        | Version (Number major, Latest, _, Prerelease prerelease)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Prerelease = prerelease)        
        | Version (Number major, Number minor, Latest, Prerelease prerelease)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor && item.Prerelease = prerelease)        
        | Version (Number major, Number minor, Number patch, Prerelease prerelease)
            -> allVersions |> Seq.tryFind (fun item -> item.Major = major && item.Minor = minor && item.Patch = patch && item.Prerelease = prerelease)

    let versionResolver (requested : RequestedAsset) = (
       let resolved = match requested.Source with
                      | Official -> findInOfficialFeed requested
                      | Staging -> findInStagingFeed requested
                      | Snapshot -> findInStagingFeed requested
       resolved
    )

    let resolve (candidate : string) = (
        match candidate with
        | RequestedAssetMatch matched -> versionResolver matched.Value
        | _ -> failwithf "Not a valid version: %s" candidate
    )

    type ResolvedVersionAssets(versions:ResolvedVersion list) =

        member this.Versions = versions

        member private this.DownloadUrl (version : ResolvedVersion) =
            match version.RequestedAsset.Product with
            | Elasticsearch ->
                match version.RequestedAsset.Source with
                | Official ->
                    sprintf "%s/elasticsearch/elasticsearch-%s.zip" ArtifactDownloadsUrl version.FullVersion
                | Staging
                | Snapshot ->
                    sprintf "%s/%s/%s-%s.msi" ArtifactDownloadsUrl this.Name this.Name version.FullVersion 
            | Kibana ->
                match version.RequestedAsset.Source with
                | Official ->
                    sprintf "%s/kibana/kibana-%s-windows-x86.zip" ArtifactDownloadsUrl version.FullVersion 
                | Staging
                | Snapshot ->
                    sprintf ""

            //match version.RequestedAsset.Source with
            //| Versions.Official ->
            //    match version.RequestedAsset.Product with
            //    | Product.Elasticsearch ->
            //        sprintf "%s/elasticsearch/elasticsearch-%s.zip" ArtifactDownloadsUrl version.FullVersion
            //    | Product.Kibana ->               
            //        sprintf "%s/kibana/kibana-%s-windows-x86.zip" ArtifactDownloadsUrl version.FullVersion 
            //| Versions.Staging
            //| Versions.Snapshot ->
            //    sprintf "%s/%s/%s-%s.msi" ArtifactDownloadsUrl this.Name this.Name version.FullVersion 
            //| BuildCandidate hash ->
            //    if (version.FullVersion.EndsWith("snapshot", StringComparison.OrdinalIgnoreCase))
            //    then SnapshotDownloadsUrl this.Name (sprintf "%i.%i.%i" version.Major version.Minor version.Patch) hash version.FullVersion
            //    else StagingDownloadsUrl this.Name hash version.FullVersion

        member private this.ZipFile (version:ResolvedVersion) =
            let fullPathInDir = InDir |> Path.GetFullPath
            Path.Combine(fullPathInDir, sprintf "%s-%s.zip" this.Name version.FullVersion)

        member private this.ExtractedDirectory (version:ResolvedVersion) =
            let fullPathInDir = InDir |> Path.GetFullPath            
            Path.Combine(fullPathInDir, sprintf "%s-%s" this.Name version.FullVersion)

        member this.BinDirs = 
            this.Versions
            |> List.filter (fun v -> v.Source = Compile)
            |> List.map(fun v -> InDir @@ sprintf "%s-%s/bin/" this.Name v.FullVersion)

        member this.ServiceDir =
            ProcessHostsDir @@ sprintf "Elastic.ProcessHosts.%s/" this.Title

        member this.ServiceBinDir = this.ServiceDir @@ "bin/AnyCPU/Release/"

        member this.DownloadPath (version:ResolvedVersion) =
            let fullPathInDir = InDir |> Path.GetFullPath 
            let releaseFile version dir =
                let downloadUrl = this.DownloadUrl version     
                Path.Combine(fullPathInDir, dir, Path.GetFileName downloadUrl)
            match version.Source with
            | Compile ->  this.ZipFile version
            | Released -> releaseFile version "releases"
            | BuildCandidate hash -> releaseFile version hash

        member this.Download () =
            this.Versions
            |> List.iter (fun version ->
                match (this.DownloadUrl version, this.DownloadPath version) with
                | (_, file) when fileExists file ->
                    tracefn "Already downloaded %s to %s" this.Name file
                | (url, file) ->
                    tracefn "Downloading %s from %s" this.Name url 
                    let targetDirectory = file |> Path.GetDirectoryName
                    if (directoryExists targetDirectory |> not) then CreateDir targetDirectory
                    use webClient = new System.Net.WebClient()
                    (url, file) |> webClient.DownloadFile
                    tracefn "Done downloading %s from %s to %s" this.Name url file 

                match version.Source with
                | Compile -> 
                    let extractedDirectory = this.ExtractedDirectory version
                    let zipFile = this.DownloadPath version
                    if directoryExists extractedDirectory |> not
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
                | _ -> ()
            )
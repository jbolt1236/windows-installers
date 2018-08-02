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
open Products

module Versions =

    type Distribution =
        | MSI
        | Zip

    type Source =
        | Official
        | Staging

    let (|DistributionMatch|) (candidate:string) =
        match candidate with
        | "msi" -> MSI
        | "zip" -> Zip
        | _ -> failwith "Not a valid distribution"

    let (|SourceMatch|) (candidate:string) =
        match candidate with
        | "official" -> Official
        | "staging" -> Staging
        | _ -> failwith "Not a valid source"

    let (|ProductMatch|) (candidate:string) =
        match candidate with
        | "e"
        | "es"
        | "elasticsearch" -> Products.Elasticsearch
        | "k"
        | "kibana" -> Products.Kibana
        | _ -> failwith "Not a valid product"

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

    type ResolvedVersion = {
        Product : string;
        FullVersion : string;
        Major : int;
        Minor : int;
        Patch : int;
        Prerelease : string;
        Hash : string;
    }

    let (|Int|_|) str =
       match System.Int32.TryParse(str) with
       | (true,int) -> Some int
       | _ -> None

    let (|RequestedVersionPartMatch|) (candidate:string) =
        match candidate with
        | Int number -> Number number
        | "*" -> Latest
        | _ -> failwith "Not a valid version"

    let (|HashMatch|_|) (candidate:string) =
        if candidate = null then None
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
        if candidate = null then Version (Latest, Latest, Latest, Stable)
        else
            let prereleasePart = PrereleasePart candidate
            let numberedPart = candidate.Split('-') |> Seq.head
            match numberedPart.Split('.') with
            | [|RequestedVersionPartMatch major; RequestedVersionPartMatch minor; RequestedVersionPartMatch patch;|]
                when (major <> Latest && minor <> Latest) || (minor = Latest && patch = Latest)
                -> Version (major, minor, patch, prereleasePart)
            | [|RequestedVersionPartMatch major; RequestedVersionPartMatch minor;|]
                when major <> Latest
                -> Version (major, minor, Latest, prereleasePart)
            | [|RequestedVersionPartMatch major;|]
                -> Version (major, Latest, Latest, prereleasePart)
            | [|RequestedVersionPartMatch major;|]
                -> Version (major, Latest, Latest, prereleasePart)
            | _ -> failwith "Not a valid specific version"

    type RequestedAsset =
        struct
            val Product : Products.Product;
            val Version : RequestedVersion;
            val Distribution : Distribution;
            val Source : Source;
            new (product, version, distribution, source) = { Product = product; Version = version; Distribution = distribution; Source = source }
        end

    let requestedAsset (candidate:string) =
        if candidate = null || candidate = "" then new RequestedAsset (Products.Elasticsearch, RequestedVersion.Version (Latest, Latest, Latest, Stable), Zip, Official)
        else
            match candidate.Split(':') with
            | [|ProductMatch product; HashMatch hash|] -> new RequestedAsset (product, Hash hash, Zip, Staging)
            | [|ProductMatch product; RequestedVersionMatch version; DistributionMatch distribution; SourceMatch source|] -> new RequestedAsset (product, version, distribution, source)
            | [|ProductMatch product; RequestedVersionMatch version; DistributionMatch distribution|] -> new RequestedAsset (product, version, distribution, Official)
            | [|ProductMatch product; HashMatch hash |] -> new RequestedAsset (product, Hash hash, Zip, Staging)
            | [|ProductMatch product; RequestedVersionMatch version;|] -> new RequestedAsset (product, version, Zip, Official)
            | [|ProductMatch product; |] -> new RequestedAsset (product, RequestedVersion.Latest, Zip, Official)
            | _ -> failwith "Not a valid requested asset"

    [<Literal>]
    let private feedUrl = "https://www.elastic.co/downloads/past-releases/feed"

    [<Literal>]
    let private feedExample = "feed-example.xml"
    
    type DownloadFeed = XmlProvider< feedExample >

    type VersionRegex = Regex< @"^(?:\s*(?<Product>.*?)\s*)?((?<Source>\w*)\:)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)", noMethodPrefix=true >

    let parseVersion (version:string, hash:string) =
        let m = VersionRegex().Match version
        if m.Success |> not then failwithf "Could not parse version from %s" version
        { Product = m.Product.Value;
          FullVersion = m.Version.Value;
          Major = m.Major.Value |> int;
          Minor = m.Minor.Value |> int;
          Patch = m.Patch.Value |> int;
          Prerelease = m.Prerelease.Value;
          Hash = hash}
          
    let private findInOfficialFeed (requested : RequestedAsset) =
        let feed = DownloadFeed.Load feedUrl
        let allVersions = feed.Channel.Items
                          |> Seq.filter (fun item -> item.Title.StartsWith(requested.Product.Title))
                          |> Seq.map (fun item -> parseVersion (item.Title, ""))                   
                          |> Seq.filter (fun version -> version.Product = requested.Product.Title)
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
                          |> Seq.map (fun item -> parseVersion (snd item, fst item))
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
       match requested.Source with
       | Official -> findInOfficialFeed requested
       | Staging -> findInStagingFeed requested
    )

    let resolve (candidate : string) = (
       let requested = requestedAsset candidate
       versionResolver requested
    )
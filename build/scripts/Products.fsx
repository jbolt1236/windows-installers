#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"

open System
open System.Globalization
open System.IO
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

    let StagingDownloadsUrl product hash fullVersion = 
        sprintf "https://staging.elastic.co/%s-%s/downloads/%s/%s-%s.msi" fullVersion hash product product fullVersion

    let SnapshotDownloadsUrl product versionNumber hash fullVersion =
        sprintf "https://snapshots.elastic.co/%s-%s/downloads/%s/%s-%s.msi" versionNumber hash product product fullVersion

module Products =
    open Paths

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

        member this.Title =
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase this.Name

    type Source =
        | Compile
        | Released
        | BuildCandidate of hash:string

        member this.Description =
            match this with
            | Compile -> "compiled from source"
            | Released -> "official release"
            | BuildCandidate hash -> sprintf "build candidate %s" hash
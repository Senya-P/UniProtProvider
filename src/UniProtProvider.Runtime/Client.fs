namespace Client

open FSharp.Json
open System.Net.Http
open System.Net.Http.Headers
open Types

module UniProtClient = 
    let resultSize = 5
    type Params(keyword : string) =
        member x.keyword = keyword
        [<DefaultValue>] val mutable organism : string
        [<DefaultValue>] val mutable cursor : string
        member this.Clone() = this.MemberwiseClone() :?> Params

    let parseLinkHeader (headers: HttpResponseHeaders) =
        if headers.Contains("Link") then
            let linkHeader = headers.GetValues("Link") |> Seq.head
            let regex = System.Text.RegularExpressions.Regex("cursor=([^&]+)")
            let matchResult = regex.Match(linkHeader)
            if matchResult.Success then Some(matchResult.Groups.[1].Value)
            else None
        else None


    let request (url: string) = async {
        use client = new HttpClient()
        let! result = client.GetStringAsync(url) |> Async.AwaitTask
        return result
    }


    let getProteinById (id: string) =
        let parts = [| "https://rest.uniprot.org/uniprotkb/search?query="; id; "&format=json" |]
        let url = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
        let jsonTask = request url
        let json = Async.RunSynchronously jsonTask
        let prot = Json.deserializeEx<Result> config json
        prot.results[0]

    let getHashedValue(url:string) =
        let cityHash = System.Data.HashFunction.CityHash.CityHashFactory.Instance.Create(System.Data.HashFunction.CityHash.CityHashConfig(HashSizeInBits = 64))
        let hash = cityHash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(url))
        hash.AsHexString()

    let getPath (url: string) =
        let hashedValue = getHashedValue url

        let mutable currentDirectory = System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory())
        while currentDirectory.Parent <> null && currentDirectory.Name <> "UniProtProvider" do
            currentDirectory <- currentDirectory.Parent

        let path = System.IO.Path.Combine(currentDirectory.FullName, "tmp")
        if not (System.IO.Directory.Exists(path)) then
            System.IO.Directory.CreateDirectory(path) |> ignore

        System.IO.Path.Combine(path, hashedValue)

    let cacheResult (path: string, contents: string) =
        System.IO.File.WriteAllText(path, contents)


    let getCachedResult (path: string) =
    // check timestemps?
        if System.IO.File.Exists(path) then
            System.IO.File.ReadAllText path
        else ""

    let LEFT_PARENTHESIS = "%28"
    let RIGHT_PARENTHESIS = "%29"
    let COLON = "%3A"
    let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)

    let buildUrl (param: Params) =
        let mutable parts : string list = []
        parts <- "https://rest.uniprot.org/uniprotkb/search?format=json&size=" :: parts
        parts <- string(resultSize) :: parts
        parts <- "&fields=id,protein_name" :: parts
        match param.cursor with
        | null -> ()
        | value -> parts <- "&cursor=" + value :: parts
        parts <- "&query=" :: parts
        parts <- param.keyword :: parts
        match param.organism with 
        | null -> ()
        | value ->  parts <- "+AND+" + LEFT_PARENTHESIS + "organism_name" + COLON + value + RIGHT_PARENTHESIS :: parts
        System.String.Concat(parts |> List.rev |> List.toArray)


    let getProteinsByKeyWord (param: Params) =
        let url = buildUrl param
        let path = getPath url
        let result = getCachedResult path
        if result = "" then
            let jsonTask = request url //"https://rest.uniprot.org/uniprotkb/search?format=json&size=5&query=insulin+AND+(organism_name:human)"
            let json = Async.RunSynchronously jsonTask
            cacheResult (path, json)

            Json.deserializeEx<IncompleteResult> config json
        else
            Json.deserializeEx<IncompleteResult> config result

    let getCursor (param: Params) = async {
        use client = new HttpClient()
        let url = buildUrl param
        let! response = client.GetAsync(url) |> Async.AwaitTask
        return parseLinkHeader response.Headers
    }


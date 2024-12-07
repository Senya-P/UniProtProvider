namespace Client

open FSharp.Json
open System.Net.Http
open System.Net.Http.Headers
open Types

module UniProtClient = 

    let private resultSize = 5

    type EntityType =
    | ProteinType of UniProtKBIncomplete
    | TaxonomyType of TaxonomyIncomplete
    type Entity =
    | Taxonomy = 0
    | Protein = 1

    type Params(keyword : string) =
        member x.keyword = keyword
        [<DefaultValue>] val mutable entity : Entity
        [<DefaultValue>] val mutable organism : string
        [<DefaultValue>] val mutable taxonId : string
        [<DefaultValue>] val mutable cursor : string
        member this.Clone() = this.MemberwiseClone() :?> Params

    let private parseLinkHeader (headers: HttpResponseHeaders) =
        if headers.Contains("Link") then
            let linkHeader = headers.GetValues("Link") |> Seq.head
            let regex = System.Text.RegularExpressions.Regex("cursor=([^&]+)")
            let matchResult = regex.Match(linkHeader)
            if matchResult.Success then Some(matchResult.Groups.[1].Value)
            else None
        else None


    let private request (url: string) = async {
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
        let prot = Json.deserializeEx<ProteinResult> config json
        prot.results[0]

    let getOrganismById (id: int) =
        let parts = [| "https://rest.uniprot.org/taxonomy/search?query="; string(id); "&format=json" |]
        let url = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
        let jsonTask = request url
        let json = Async.RunSynchronously jsonTask
        let prot = Json.deserializeEx<TaxonomyResult> config json
        prot.results[0]

    let private getHashedValue(url:string) =
        let cityHash = System.Data.HashFunction.CityHash.CityHashFactory.Instance.Create(System.Data.HashFunction.CityHash.CityHashConfig(HashSizeInBits = 64))
        let hash = cityHash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(url))
        hash.AsHexString()

    let private getPath (url: string) =
        let hashedValue = getHashedValue url

        let mutable currentDirectory = System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory())
        while currentDirectory.Parent <> null && currentDirectory.Name <> "UniProtProvider" do
            currentDirectory <- currentDirectory.Parent

        let path = System.IO.Path.Combine(currentDirectory.FullName, "tmp")
        if not (System.IO.Directory.Exists(path)) then
            System.IO.Directory.CreateDirectory(path) |> ignore

        System.IO.Path.Combine(path, hashedValue)

    let private cacheResult (path: string, contents: string) =
        System.IO.File.WriteAllText(path, contents)


    let private getCachedResult (path: string) =
    // check timestemps?
        if System.IO.File.Exists(path) then
            System.IO.File.ReadAllText path
        else ""

    let private LEFT_PARENTHESIS = "%28"
    let private RIGHT_PARENTHESIS = "%29"
    let private COLON = "%3A"
    let private config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)

    let private buildUrl (param: Params) =
        let mutable parts : string list = []
        parts <- "https://rest.uniprot.org/" :: parts
        match param.entity with
        | Entity.Protein -> parts <- "uniprotkb" :: parts
        | Entity.Taxonomy -> parts <- "taxonomy" :: parts
        | _ -> ()
        parts <- "/search?format=json&size=" :: parts
        parts <- string(resultSize) :: parts
        match param.entity with
        | Entity.Protein -> parts <- "&fields=id,protein_name" :: parts
        | Entity.Taxonomy -> parts <- "&fields=id,scientific_name" :: parts
        | _ -> ()
        match param.cursor with
        | null -> ()
        | value -> parts <- "&cursor=" + value :: parts
        parts <- "&query=" :: parts
        parts <- param.keyword :: parts
        match param.taxonId with 
        | null -> ()
        | value ->  parts <- LEFT_PARENTHESIS + "taxonomy_id" + COLON + value + RIGHT_PARENTHESIS :: parts
        match param.organism with 
        | null -> ()
        | value ->  parts <- "+AND+" + LEFT_PARENTHESIS + "organism_name" + COLON + value + RIGHT_PARENTHESIS :: parts
        System.String.Concat(parts |> List.rev |> List.toArray)

    let private getDeserializedResult<'T> (deserializeFunc: string -> 'T) (json: string) : 'T =
        deserializeFunc json
    let private getResultsByKeyWord (param: Params) (deserializeFunc: string -> 'T) : 'T =
        let url = buildUrl (param)
        let path = getPath url
        let result = getCachedResult path
        let json =
            if result = "" then
                let jsonTask = request url
                let json = Async.RunSynchronously jsonTask
                cacheResult (path, json)
                json
            else
                result
        getDeserializedResult deserializeFunc json

    let getProteinsByKeyWord (param: Params) =
        param.entity <- Entity.Protein
        getResultsByKeyWord param (Json.deserializeEx<UniProtKBIncompleteResult> config)

    let getOrganismsByKeyWord (param: Params) =
        param.entity <- Entity.Taxonomy
        getResultsByKeyWord param (Json.deserializeEx<TaxonomyIncompleteResult> config)
    let getCursor (param: Params) = async {
        use client = new HttpClient()
        let url = buildUrl param 
        let! response = client.GetAsync(url) |> Async.AwaitTask
        return parseLinkHeader response.Headers
    }


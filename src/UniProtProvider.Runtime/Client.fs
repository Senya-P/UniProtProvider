namespace Client

open FSharp.Json
open System.Net.Http
open System.Net.Http.Headers
open Types
open System.IO.Compression
open System.IO

// --------------------------------------------------------------------------------------
// Handles the communication with the UniProt API and caching of the results
// --------------------------------------------------------------------------------------
module UniProtClient = 
    let private resultSize = 50

    type EntityType =
    | ProteinType of UniProtKBIncomplete
    | TaxonomyType of TaxonomyIncomplete
    type Entity =
    | Taxonomy = 0
    | Protein = 1

    /// Represents parameters for the query
    type Params(keyword : string) =
        member x.keyword = keyword
        [<DefaultValue>] val mutable entity : Entity
        [<DefaultValue>] val mutable organism : string
        [<DefaultValue>] val mutable taxonId : string
        [<DefaultValue>] val mutable cursor : string
        member this.Clone() = this.MemberwiseClone() :?> Params

    /// Parses the Link header from the HTTP response to extract the cursor value
    let private parseLinkHeader (headers: HttpResponseHeaders) =
        if headers.Contains("Link") then
            let linkHeader = headers.GetValues("Link") |> Seq.head
            let regex = System.Text.RegularExpressions.Regex("cursor=([^&]+)")
            let matchResult = regex.Match(linkHeader)
            if matchResult.Success then 
                let cursor = matchResult.Groups.[1].Value
                cursor
            else 
                ""
        else 
            ""

    /// Sends a GET request to the specified URL
    let private request (url: string) =  
        use client = new HttpClient()
        let response = client.GetAsync(url)
        response.Result.Content.ReadAsStreamAsync().Result
    let getProteinById (id: string) =
        let parts = [| "https://rest.uniprot.org/uniprotkb/search?query="; id; "&format=json" |]
        let url = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
        let jsonStream = request url
        let reader = new StreamReader(jsonStream)
        let json = reader.ReadToEnd()
        let prot = Json.deserializeEx<ProteinResult> config json
        prot.results[0]

    let getOrganismById (id: int) =
        let parts = [| "https://rest.uniprot.org/taxonomy/search?query="; string(id); "&format=json" |]
        let url = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
        let jsonStream = request url
        let reader = new StreamReader(jsonStream)
        let json = reader.ReadToEnd()
        let prot = Json.deserializeEx<TaxonomyResult> config json
        prot.results[0]

    let private getHashedValue(url:string) =
        let cityHash = System.Data.HashFunction.CityHash.CityHashFactory.Instance.Create(System.Data.HashFunction.CityHash.CityHashConfig(HashSizeInBits = 64))
        let hash = cityHash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(url))
        hash.AsHexString()

    /// Constructs the file path for caching based on the hashed URL
    let private getPath (url: string) =
        let hashedValue = getHashedValue url
        let mutable currentDirectory = System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory())
        let path = System.IO.Path.Combine(currentDirectory.FullName, "tmp")
        if not (System.IO.Directory.Exists(path)) then
            System.IO.Directory.CreateDirectory(path) |> ignore

        System.IO.Path.Combine(path, hashedValue)

    let private cacheResult (path: string, contents: Stream) =
        use fileStream = new FileStream(path, FileMode.Create, FileAccess.Write)
        contents.CopyTo(fileStream)


    let private getCachedResult (path: string) =
    // check timestemps and delete old?
        if File.Exists(path) then
            use compressedFileStream = File.Open(path, FileMode.Open, FileAccess.Read)
            use decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress)
            use result =  new StreamReader(decompressor)
            let decompressed = result.ReadToEnd()
            decompressed
        else ""

    let private LEFT_PARENTHESIS = "%28"
    let private RIGHT_PARENTHESIS = "%29"
    let private COLON = "%3A"
    let private config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)

    /// Builds the URL for the query based on the dataset type and parameters
    let private buildUrl (param: Params) =
        let mutable parts : string list = []
        parts <- "https://rest.uniprot.org/" :: parts
        match param.entity with
        | Entity.Protein -> parts <- "uniprotkb" :: parts
        | Entity.Taxonomy -> parts <- "taxonomy" :: parts
        | _ -> ()
        parts <- "/search?compressed=true&format=json&size=" :: parts
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
        let result = System.String.Concat(parts |> List.rev |> List.toArray)
        result

    let private getDeserializedResult<'T> (deserializeFunc: string -> 'T) (json: string) : 'T =
        deserializeFunc json

    let private getResults (param: Params) (deserializeFunc: string -> 'T) : 'T =
        let url = buildUrl param
        let path = getPath url
        let result = getCachedResult path
        let json =
            if result = "" then
                use json = request url
                cacheResult(path, json)
                getCachedResult path
            else
                result
        getDeserializedResult deserializeFunc json

    let getProteinsByKeyWord (param: Params) =
        param.entity <- Entity.Protein
        getResults param (Json.deserializeEx<UniProtKBIncompleteResult> config)

    let getOrganismsByKeyWord (param: Params) =
        param.entity <- Entity.Taxonomy
        getResults param (Json.deserializeEx<TaxonomyIncompleteResult> config)

    /// Retrieves the cursor for the next set of results
    let getCursor (param: Params) = 
        use client = new HttpClient()
        let url = buildUrl param 
        let response = client.GetAsync(url).Result
        let cursor = parseLinkHeader response.Headers
        cursor



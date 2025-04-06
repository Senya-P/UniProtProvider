module Client

open FSharp.Json
open System.Net.Http
open System.Net.Http.Headers
open Cache
open System.IO

// --------------------------------------------------------------------------------------
// Handles the communication with the UniProt API and caching of the results
// --------------------------------------------------------------------------------------
let private resultSize = 50
let private client = new HttpClient()
let private config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)

type Entity =
| Taxonomy
| Protein

/// Represents parameters for the query
type Params(keyword : string) =
    member x.keyword = keyword
    [<DefaultValue>] val mutable entity : Entity
    [<DefaultValue>] val mutable organism : string
    [<DefaultValue>] val mutable taxonId : string
    [<DefaultValue>] val mutable cursor : string
    [<DefaultValue>] val mutable reviewed : bool
    [<DefaultValue>] val mutable ProteinExistence : ProteinExistence

    member this.Clone() = this.MemberwiseClone() :?> Params


module private Helpers = 

    /// Parses the Link header from the HTTP response to extract the cursor value
    let parseLinkHeader (headers: HttpResponseHeaders) =
        if headers.Contains "Link" then
            let linkHeader = headers.GetValues "Link" |> Seq.head
            let regex = System.Text.RegularExpressions.Regex "cursor=([^&]+)"
            let matchResult = regex.Match linkHeader
            if matchResult.Success then 
                Some matchResult.Groups.[1].Value
            else 
                None
        else 
            None

    let private LEFT_PARENTHESIS = "%28"
    let private RIGHT_PARENTHESIS = "%29"
    let private COLON = "%3A"

    /// Builds the URL for the query based on the dataset type and parameters
    let buildUrl (param: Params) =
        let mutable parts : string list = []
        parts <- "https://rest.uniprot.org/" :: parts

        match param.entity with
        | Entity.Protein -> parts <- "uniprotkb" :: parts
        | Entity.Taxonomy -> parts <- "taxonomy" :: parts

        parts <- "/search?compressed=true&format=json&size=" :: parts
        parts <- string(resultSize) :: parts

        match param.entity with
        | Entity.Protein -> parts <- "&fields=id,protein_name" :: parts
        | Entity.Taxonomy -> parts <- "&fields=id,scientific_name" :: parts

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

        match param.reviewed with
        | false -> ()
        | true -> parts <- "+AND+" + LEFT_PARENTHESIS + "reviewed" + COLON + "true" + RIGHT_PARENTHESIS :: parts

        if param.ProteinExistence <> ProteinExistence.None then
            parts <- "+AND+" + LEFT_PARENTHESIS + "existence" + COLON + string(int param.ProteinExistence) + RIGHT_PARENTHESIS :: parts

        let result = System.String.Concat(parts |> List.rev |> List.toArray)
        result

/// Sends a GET request to the specified URL
let private request (url: string) =  async {
    let! response =  Async.AwaitTask(client.GetAsync(url))
    return! response.Content.ReadAsStreamAsync() |> Async.AwaitTask
}

let getProteinById (id: string) = async {
    let url = "https://rest.uniprot.org/uniprotkb/search?query=" + id + "&format=json"
    let! responseStream = request url
    use reader = new StreamReader(responseStream)
    let! json = reader.ReadToEndAsync() |> Async.AwaitTask
    let prot = Json.deserializeEx<ProteinResult> config json
    return prot.results[0]
}

let getOrganismById (id: int) = async {
    let url = "https://rest.uniprot.org/taxonomy/search?query=" + string(id) + "&format=json"
    let! responseStream = request url
    use reader = new StreamReader(responseStream)
    let! json = reader.ReadToEndAsync() |> Async.AwaitTask
    let prot = Json.deserializeEx<TaxonomyResult> config json
    return prot.results[0]
}

let private getResults<'T> (entity: Entity) (param: Params)  = async {
    let url =  Helpers.buildUrl param
    let! cachedJson = getCachedResult url

    if cachedJson = "" then
        let! json = request url
        do! cacheResult(url, json)
        let! result =  getCachedResult url
        return Json.deserializeEx<'T> config result
    else
        return Json.deserializeEx<'T> config cachedJson
}

let getProteinsByKeyWord (param: Params) =
    param.entity <- Entity.Protein
    getResults<UniProtKBIncompleteResult> Protein param

let getOrganismsByKeyWord (param: Params) =
    param.entity <- Entity.Taxonomy
    getResults<TaxonomyIncompleteResult>  Taxonomy param 

/// Retrieves the cursor for the next set of results
let getCursor (param: Params) = async {
    let url = Helpers.buildUrl param 
    let! response = client.GetAsync(url) |> Async.AwaitTask
    return Helpers.parseLinkHeader response.Headers
}

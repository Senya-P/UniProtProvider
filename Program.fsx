//#r "src/UniProtProvider.DesignTime/bin/Debug/netstandard2.1/UniProtProvider.DesignTime.dll"
#r "nuget: FSharp.Json, 0.4.1"
open FSharp.Json
open System.Net.Http
type ProtIncomplete =
    {
        primaryAccession : string
        uniProtkbId : string
    }
type IncompleteResult =
    {
        results : array<ProtIncomplete>
    }

let request (query: string) =
        let client = new HttpClient()
        let response = client.GetStringAsync(query)
        response.Result
let keyWord = "human"
let parts = [| "https://rest.uniprot.org/uniprotkb/search?query="; keyWord; "&format=json&size=100" |]
let query = System.String.Concat(parts)
let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
let json = request query
let prot = Json.deserializeEx<IncompleteResult> config json

let arr = prot.results
for i in arr do
    printf "%s " i.primaryAccession
    printf "%s\n" i.uniProtkbId

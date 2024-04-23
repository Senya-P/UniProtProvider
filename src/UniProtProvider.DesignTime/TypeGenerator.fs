namespace UniProtProvider.DesignTime
open FSharp.Json
open System.Net.Http
module TypeGenerator =

    type Result = 
        {
            results : array<ProtSeq>
        }
    and ProtSeq = 
        {
            uniParcId : string
            uniParcCrossReferences: obj
            sequence : Sequence
            sequenceFeatures: array<SequenceFeature>
            oldestCrossRefCreated : string
            mostRecentCrossRefUpdated : string
         }
    and Sequence = 
        {
            value : string
            length : int
            molWeight: int
            crc64 : string
            md5 : string
        }
    and SequenceFeature =
        {
            interproGroup : InterproGroup
            database : string
            databaseId : string
            locations : array<Location>
        }
    and InterproGroup = 
        {
            id : string
            name : string
        }
    and Location = 
        {
            start : int
            ``end`` : int
        }
    let request (query: string) =
        let client = new HttpClient()
        let response = client.GetStringAsync(query)
        response.Result

    let genType (id: string) =
        let parts = [| "https://rest.uniprot.org/uniparc/search?query="; id; "&format=json" |]
        let query = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true)
        let json = request query
        let protSeq = Json.deserializeEx<Result> config json
        protSeq.results[0]


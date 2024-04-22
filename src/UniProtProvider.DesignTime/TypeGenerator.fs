namespace UniProtProvider.DesignTime
open ProviderImplementation.ProvidedTypes
open Newtonsoft.Json

[<AutoOpen>]
module TypeGenerator =

    type ProtSeq = 
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
            starts : int
            ends : int
        }

    let genType (id: string) =
        let parts = [| "https://rest.uniprot.org/uniparc/search?query="; id; "&format=json" |]
        let query = System.String.Concat(parts)
        let protSeq = JsonConvert.DeserializeObject<ProtSeq>(query)
        protSeq

namespace UniProtProvider.RunTime

open System
open FSharp.Json
open System.Net.Http
// Put any utilities here
[<AutoOpen>]
module internal Utilities = 
    let x = 1


// Put any runtime constructs here
type DataSource(filename:string) = 
    member this.FileName = filename

type Organism = 
    {
        scientificName : string
        commonName : string
        taxonId : int
        lineage : array<string>
    }
type Audit = 
    {
        firstPublicDate : string
        lastAnnotationUpdateDate : string
        lastSequenceUpdateDate : string
        entryVersion : int
        sequenceVersion : int
    }


type Point = 
    {
        value : int
        modifier : string
    }
type Location = 
    {
        start : Point
        ``end`` : Point
    }

type Evidence = 
    {
        evidenceCode : string
        source : string
        id : int
    }
    // may be split with and without evidences
type Fullname = 
    {
        value : string
        evidences : array<Evidence>
    }
type Name = 
    {
        fullname: Fullname
        shortNames : array<Fullname>
    }

type Gen = 
    {
        geneName : Fullname
        orfNames : array<Fullname>
    }
type Description = 
    {
        recommendedName : Name
        alternativeNames: array<Name>
    }
type Feature = 
    {
        ``type`` : string
        location : Location
        description : string
        featureId : string
        evidences : array<Evidence>
    }
type Keyword = 
    {
        id : string
        category : string
        name : string
    }

type Property = 
    {
        key : string
        value : string
    }
type CrossReference =
    {
        database : string
        id : string
        properties : array<Property>
    }
type Citation = 
    {
        id : string
        citationType : string
        authors : array<string>
        citationCrossReferences : array<CrossReference>
        title : string
        publicationDate : string
        journal : string
        firstPage : string
        lastPage : string
        volume : string
    }
type ReferenceComment =
    {
        value : string
        ``type`` : string
    }
type Reference = 
    {
        referenceNumber : int
        citation : Citation
        referencePositions : array<string>
        referenceComments : array<ReferenceComment>
    }
type Sequence = 
    {
        value : string
        length : int
        molWeight : int
        crc64 : string
        md5 : string
    }
type Attributes =
    { 
        countByCommentType : array<(string * int)>
        countByFeatureType : array<(string * int)>
        uniParcId : string
    }
type Text = 
    {
        evidences : array<Evidence>
        value : string
    }
type Isoform = 
    {
        name: Fullname
        isoformIds : array<string>
        isoformSequenceStatus : string

    }
type Comment =
    {
        texts : array<Text>
        commentType : string
        events : array<string>
        isoforms : array<Isoform>
    }
type Prot = 
    {
        entryType : string
        primaryAccession : string
        secondaryAccessions : array<string>
        uniProtkbId : string
        entryAudit : Audit
        annotationScore : double
        organism : Organism
        organismHosts : array<Organism> //
        proteinExistence : string
        proteinDescription : Description
        genes : array<Gen>
        comments : array<Comment> //
        features : array<Feature>
        keywords : array<Keyword>
        references : array<Reference>
        uniProtKBCrossReferences : array<CrossReference>
        sequence : Sequence
        extraAttributes : Attributes
    }
type Result = 
    {
        results : array<Prot>
    }
type ProtIncomplete =
    {
        primaryAccession : string
        uniProtkbId : string
    }
type IncompleteResult =
    {
        results : array<ProtIncomplete>
    }

module TypeGenerator = 
    open ProviderImplementation.ProvidedTypes
    let request (query: string) =
        let client = new HttpClient()
        let response = client.GetStringAsync(query)
        response.Result

    let genTypeById (id: string) =
        let parts = [| "https://rest.uniprot.org/uniprotkb/search?query="; id; "&format=json" |]
        let query = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true)
        let json = request query
        let prot = Json.deserializeEx<Result> config json
        prot.results[0]

    let genTypesByKeyWord (keyWord: string) = 
        let parts = [| "https://rest.uniprot.org/uniprotkb/search?query="; keyWord; "&format=json&size=5" |]
        let query = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
        let json = request query
        let prot = Json.deserializeEx<IncompleteResult> config json
        prot.results



// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly:CompilerServices.TypeProviderAssembly("UniProtProvider.DesignTime.dll")>]
do ()
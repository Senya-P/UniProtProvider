namespace UniProtProvider.RunTime

open System
open FSharp.Json
open System.Net.Http
open System.Net.Http.Headers

// Put any utilities here
[<AutoOpen>]
module internal Utilities = 
    let x = 1


// Put any runtime constructs here
type DataSource(filename:string) = 
    member this.FileName = filename


type SomeRuntimeHelper() = 
    static member Help() = "help"

[<AllowNullLiteral>]
type SomeRuntimeHelper2() = 
    static member Help() = "help"

type Server (name : string) =
    member x.Name with get() : string = name

type Organism = 
    {
        scientificName : string option
        commonName : string option
        taxonId : int option
        lineage : array<string> option
    }
type Audit = 
    {
        firstPublicDate : string option
        lastAnnotationUpdateDate : string option
        lastSequenceUpdateDate : string option
        entryVersion : int option
        sequenceVersion : int option
    }


type Point = 
    {
        value : int option
        modifier : string option
    }
type Location = 
    {
        start : Point option
        ``end`` : Point option
    }

type Evidence = 
    {
        evidenceCode : string option
        source : string option
        id : string option
    }
    // may be split with and without evidences
type Fullname = 
    {
        value : string
        evidences : array<Evidence> option
    }
type Name = 
    {
        fullName: Fullname
        shortNames : array<Fullname> option
    }

type Gen = 
    {
        geneName : Fullname option
        orfNames : array<Fullname> option
    }
type Description = 
    {
        recommendedName : Name option
        alternativeNames: array<Name> option
    }
type Feature = 
    {
        ``type`` : string option
        location : Location option
        description : string option
        featureId : string option
        evidences : array<Evidence> option
    }
type Keyword = 
    {
        id : string option
        category : string option
        name : string option
    }

type Property = 
    {
        key : string option
        value : string option
    }
type CrossReference =
    {
        database : string option
        id : string option
        properties : array<Property> option
    }
type Citation = 
    {
        id : string option
        citationType : string option
        authors : array<string> option
        citationCrossReferences : array<CrossReference> option
        title : string option
        publicationDate : string option
        journal : string option
        firstPage : string option
        lastPage : string option
        volume : string option
    }
type ReferenceComment =
    {
        value : string option
        ``type`` : string option
    }
type Reference = 
    {
        referenceNumber : int option
        citation : Citation option
        referencePositions : array<string> option
        referenceComments : array<ReferenceComment> option
    }
type Sequence = 
    {
        value : string option
        length : int option
        molWeight : int option
        crc64 : string option
        md5 : string option
    }
type Attributes =
    { 
        //countByCommentType : array<(string * int)> option
        //countByFeatureType : array<(string * int)> option
        uniParcId : string option
    }
type Text = 
    {
        evidences : array<Evidence> option
        value : string option
    }
type Isoform = 
    {
        name: Fullname option
        isoformIds : array<string> option
        isoformSequenceStatus : string option

    }
type Comment =
    {
        texts : array<Text> option
        commentType : string option
        events : array<string> option
        isoforms : array<Isoform> option
    }
type Prot = 
    {
        entryType : string option
        primaryAccession : string
        secondaryAccessions : array<string> option
        uniProtkbId : string
        entryAudit : Audit option
        annotationScore : double option
        organism : Organism option
        organismHosts : array<Organism> option
        proteinExistence : string option
        proteinDescription : Description
        genes : array<Gen> option
        comments : array<Comment> option
        features : array<Feature> option
        keywords : array<Keyword> option
        references : array<Reference> option
        uniProtKBCrossReferences : array<CrossReference> option
        sequence : Sequence option
        extraAttributes : Attributes option
    }
type Result = 
    {
        results : array<Prot>
    }
type ProtIncomplete =
    {
        //primaryAccession : string
        uniProtkbId : string
        proteinDescription : Description
    }
type Suggestion =
    {
        query : string option
        hits : obj option
    }
type IncompleteResult =
    {
        results : array<ProtIncomplete>
        suggestions: array<Suggestion> option
    }

module TypeGenerator = 

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
            let match_ = regex.Match(linkHeader)
            if match_.Success then Some(match_.Groups.[1].Value)
            else None
        else None


    let request (query: string) =
        let client = new HttpClient()
        client.GetStringAsync(query).Result

    let genTypeById (id: string) =
        let parts = [| "https://rest.uniprot.org/uniprotkb/search?query="; id; "&format=json" |]
        let query = System.String.Concat(parts)
        let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
        let json = request query
        let prot = Json.deserializeEx<Result> config json
        prot.results[0]

    let buildQuery (param: Params) =
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
        | value ->  parts <- "+AND+(organism_name:" + value + ")" :: parts
        let query = System.String.Concat(parts |> List.rev |> List.toArray)
        query

    let genTypesByKeyWord (param: Params) = 
        let query = buildQuery param
        let config = JsonConfig.create(allowUntyped = true, deserializeOption = DeserializeOption.AllowOmit)
        let json = request query //"https://rest.uniprot.org/uniprotkb/search?format=json&size=5&query=insulin+AND+(organism_name:human)"
        let prot = Json.deserializeEx<IncompleteResult> config json
        prot

    let getCursor (param: Params) =
        let client = new HttpClient()
        let query = buildQuery param
        let response = client.GetAsync(query)
        parseLinkHeader response.Result.Headers


// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly:CompilerServices.TypeProviderAssembly("UniProtProvider.DesignTime.dll")>]
do ()
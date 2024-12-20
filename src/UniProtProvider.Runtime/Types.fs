namespace Types

type DataSource(filename:string) = 
    member this.FileName = filename

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
        fullName : Fullname
        ecNumbers : array<Fullname> option
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
        cdAntigenNames : array<Fullname> option
        alternativeNames : array<Name> option
        contains : array<Description> option
        flag : string option
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
type Protein = 
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
type TaxonomyIncomplete =
    {
        scientificName : string
        taxonId : int
    }
type Lineage =
    {
        scientificName : string option
        commonName : string option
        taxonId : int option
        rank : string option
        hidden : bool option
    }
type Taxonomy =
    {
        scientificName : string
        commonName : string option
        taxonId : int
        mnemonic : string option
        parent : TaxonomyIncomplete option
        rank : string option
        hidden : bool option
        active : bool option
        otherNames : array<string> option
        lineage : array<Lineage> option
        links : array<string> option
    }
type ProteinResult = 
    {
        results : array<Protein>
    }
type TaxonomyResult = 
    {
        results : array<Taxonomy>
    }

type UniProtKBIncomplete =
    {
        uniProtkbId : string
        proteinDescription : Description
    }
type Suggestion =
    {
        query : string option
        hits : obj option
    }

type UniProtKBIncompleteResult =
    {
        results : array<UniProtKBIncomplete>
        suggestions: array<Suggestion> option
    }
type TaxonomyIncompleteResult = 
    {
        results: array<TaxonomyIncomplete>
    }

namespace Types
// --------------------------------------------------------------------------------------
// Types used to back the UniProKB data schema
// Retrieved from: https://www.uniprot.org/api-documentation/uniprotkb#schemas
// --------------------------------------------------------------------------------------

type DataSource(filename:string) = 
    member this.FileName = filename

type Evidence = 
    {
        evidenceCode : string option
        source : string option
        id : string option
    }

type Organism = 
    {
        scientificName : string option
        commonName : string option
        taxonId : int option
        lineages : array<string> option
        synonyms : array<string> option
        evidences : array<Evidence> option
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
        sequence : string option
    }
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
        valid : bool option
    }

type Gene = 
    {
        geneName : Fullname option
        orfNames : array<Fullname> option
        synonyms : array<Fullname> option
        orderedLocusNames : array<Fullname> option
    }
type GeneLocation = 
    {
        geneEncodingType : string option
        value : string option
        evidences : array<Evidence> option
    }
type Description = 
    {
        recommendedName : Name option
        cdAntigenNames : array<Fullname> option
        innNames : array<Fullname> option
        alternativeNames : array<Name> option
        submissionNames : array<Name> option
        contains : array<Description> option
        includes : array<Description> option
        flag : string option
        allergenName : Fullname option
        biotechName : Fullname option
    }
type Ligand = 
    {
        id : string option
        name : string option
        note : string option
        label : string option
    }
type AlternativeSequence = 
    {
        originalSequence : string option
        alternativeSequences : array<string> option
    }
type Feature = 
    {
        ``type`` : string option
        location : Location option
        description : string option
        featureId : string option
        evidences : array<Evidence> option
        ligand : Ligand option
        ligandPart : Ligand option
        alternativeSequence : AlternativeSequence option
    }
type Keyword = 
    {
        id : string option
        category : string option
        name : string option
        evidences : array<Evidence> option
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
        isoformId : string option
        evidences : array<Evidence> option
    }
type Citation = 
    {
        id : string option
        citationType : string option
        authors : array<string> option
        bookName : string option
        address : string option
        citationCrossReferences : array<CrossReference> option
        title : string option
        publicationDate : string option
        publisher : string option
        pubmedId : int option
        journal : string option
        locator : string option
        literatureAbstract : string option
        firstPage : string option
        lastPage : string option
        volume : string option
        institute : string option
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
        evidences : array<Evidence> option
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
        // incomplete
    }
type Lineage =
    {
        scientificName : string option
        commonName : string option
        taxonId : int option
        rank : string option
        hidden : bool option
        synonyms : array<string> option
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
        genes : array<Gene> option
        geneLocation : GeneLocation option
        comments : array<Comment> option
        features : array<Feature> option
        keywords : array<Keyword> option
        references : array<Reference> option
        uniProtKBCrossReferences : array<CrossReference> option
        sequence : Sequence option
        extraAttributes : Attributes option
        active : bool option
        fragment : bool option
        lineages : array<Lineage> option
    }
type TaxonomyIncomplete =
    {
        scientificName : string
        taxonId : int
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

# Software Project Specification
## Type providers for UniProt 

## Basic information
1. Overview and purpose of the software project

The UniProtProvider provides tools to simplify accessing and manipulating bioinformatic data. The package implements Type Providers to allow remote access to UniProt datasets and type-safe representations of their data.

2. Technologies used 
   - F# programming language
   - F# Type Provider SDK

3. External links
   - https://www.uniprot.org/
   - https://fsprojects.github.io/FSharp.TypeProviders.SDK/

## Brief description of the software
1. The reason for the development of the SW project and its basic parts and the objectives of the solution

The reason for creation of UniProtProvider is that UniProt lacks type-aware programmatic access. The UniProt API provides functionality to retrieve entries via queries in several formats such as TSV or JSON. This requires the user to parse and process the data themselves. The main porpose of the work is to provide the non-programmer user of UniProt datasets with intuitive and type-specific access to the data and its relationships.
The package consists of the following type providers:
- `UniParc provider` for protein sequences
- `UniProtKB provider` for proteins
- `UniRef provider` for protein clusters

2. Main functions
   - remote access to UniProt datasets
   - retrieving data in the type-specific representation
   - performing type-specific functions on data

3. Motivational example of use

Retrieving by UniProt id:
   ```
    let [<Literal>] id = "UPI000000000A"
    type Uniparc = UniParcProvider
    let obj = Uniparc.ById(id).sequence.md5 // "8869480F6C4AB10C36AB8943E6D9FE98"

    let [<Literal>] id = "P68452"
    type Uniprotkb = UniProtKBProvider
    let obj = Uniprotkb.ById(id).organism.commonName // "VACV"

    let [<Literal>] id = "UniRef90_Q9H9K5"
    type Uniref = UniRefProvider
    let obj = Uniref.ById(id).name // "Cluster: Endogenous retroviral envelope protein HEMO"
   ```
Retrieving by keyword:
```
type Uniprotkb = UniProtKBProvider
let obj = Uniprotkb.ByKeyWord<"Insuli">.``insulin``
                                       .``insult``
                                       .``insulz``
                                       .``insulae``
                                       .``inouei``
```


4. Environment 

The package should be included into F# project or script, so it requires F#, .NET SDK and .NET Framework installed.
TBA: include small installation guide

5. Installation

The package is available on github https://github.com/Senya-P/UniProtProvider
(Optionally as a NuGet package)

6. Limitations
The UniProtProvider package should be downloaded, included and referenced as a standalone library. It is not part of any framework or larger libraries.

## Interface
1. User interface, input and output
TBA

2. Software interface
TBA

## Detailed description of functionality
1. Creating a type
```
    type Uniparc = UniParcProvider
    type Uniprotkb = UniProtKBProvider
    type Uniref = UniRefProvider
```
2. Creating an instance
```
    let obj = Uniparc.ById(id)
```
3. Properties
   obj.name
      .sequence
      .memberCount
4. Methods
TBA: filters, grouping, ...

## Possible improvements & features
- Integration into bioinformatics library such as BioFSharp: https://github.com/CSBiology/BioFSharp/
- Adding static methods that provide useful manipulations with retrieved data.
- Adding support for Species, Taxonomy, Deseases types.

## Timeline & milestones

28.02 Topic definition
15.03 Installation of required tools, getting through documentation of type providers and F#, defining goals of the project
29.04 Specification - main part, mock-up of a UniParc type provider
15.05 Final version of specification, type provider with generation of simple types
15.06 Type provider with cross references
15.07 Implementation of UniProtKB, UniRef type providers
15.08 Fine-tuning and publication

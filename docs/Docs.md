# Software Project Specification
## Type providers for UniProt 

## Basic information
1. Overview and purpose of the software project

The UniProtProvider provides tools to simplify accessing and manipulating bioinformatic data. The package implements Type Providers to allow remote access to UniProt datasets and type-safe representations of their data.

2. Technologies used 
   - F# programming language
   - F# Type Provider SDK

3. Conventions of this document

4. External links
   - https://www.uniprot.org/
   - https://fsprojects.github.io/FSharp.TypeProviders.SDK/

## Brief description of the software
1. The reason for the development of the SW project and its basic parts and the objectives of the solution

The reason for creation of UniProtProvider is that UniProt lacks type-aware programmatic access. The UniProt API provides functionality to retrieve entries via queries in several formats such as TSV or JSON. This requires the user to parse and process the data themselves. The main porpose of the work is to provide the non-programmer user of UniProt datasets with intuitive and type-specific access to the data and its relationships.
The package consists of the following type providers:
- `UniParc provider` for protein sequences
- `UnProtKB provider` for proteins
- `UniRef provider` for protein clusters

2. Main functions
   - remote access to UniProt datasets
   - retrieving data in the type-specific representation
   - performing type-specific functions on data

3. Motivational example of use
   `code example`

4. Environment 

5. Limitations
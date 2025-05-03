# Getting started

UniProtProvider is a F# type provider for [uniprot.org](https://www.uniprot.org/)

*"UniProt is the world’s leading high-quality, comprehensive and freely accessible resource of protein sequence and functional information."*

The UniProtProvider provides access to protein entries (UniProtKB) with the support of Taxonomy data.

## Install from sources

Dependencies: .NET runtime 5.0+, change inside `UniProtProvider/global.json`

Compile the project:
```
dotnet tool restore
dotnet paket update
dotnet build
```

Run tests (requres .NET 6.0):
```
dotnet test
```

## NuGet

[Get the last version of the package](https://www.nuget.org/packages/UniProtProvider/)

Add to your project:
```
dotnet add package UniProtProvider --version 1.0.10
```

Or reference from an F# interactive (.fsi) file
```
#r "nuget: UniProtProvider, 1.0.10"
```

It is possible to use UniProtProvider inside [Polyglot notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode).

# Tutorial

### Examine individual protein entries

```
UniProtProvider.ById<"A0AVG3">()
```

### Examine individual taxonomy entries

```
UniProtProvider.ByTaxonId<9606>()
```

### Find protein entries by keyword

```
UniProtProvider.ByKeyWord<"inulin">()
    .``6(G)-fructosyltransferase (GFT_ASPOF)``
```

A list of the first fifty related entries is returned. To load more entries, ` ``More..`` ` property can be used.

If no results are found, suggested keywords may be available.

```
UniProtProvider.ByKeyWord<"kerain">().keratin
```

### Filter protein entries by organism name

```
UniProtProvider.ByKeyWord<"inulin">()
    .ByOrganism<"mouse">()
    .``Ig heavy chain V region AMPC1 (HVM34_MOUSE)``
```

### Find taxonomy entries by keyword

```
UniProtProvider.ByOrganism<"human">()
```

A list of the first fifty related entries is returned. To load more entries, ` ``More..`` ` property can be used.

### List related protein entries

```
UniProtProvider.ByOrganism<"9606">()
    .``Homo sapiens (9606)``
    .FindRelated()
```

### List only reviewed protein entries

```
UniProtProvider.ByKeyWord<"inulin">()
    .ByOrganism<"human">()
    .Reviewed()
```

### Filter protein entries by evidence type

```
UniProtProvider.ByKeyWord<"keratin">()
    .ByProteinExistence<ProteinExistence.InferredFromHomology>()
```

### Filtering options

Filtering of protein results can be applied in any order. Thus, all the following invocations are valid and return the same result.

```
UniProtProvider.ByKeyWord<"keratin">()
    .ByOrganism<"mouse">()
    .Reviewed()
    .ByProteinExistence<ProteinExistence.InferredFromHomology>()

UniProtProvider.ByKeyWord<"keratin">()
    .Reviewed()
    .ByOrganism<"mouse">()
    .ByProteinExistence<ProteinExistence.InferredFromHomology>()

UniProtProvider.ByKeyWord<"keratin">()
    .ByProteinExistence<ProteinExistence.InferredFromHomology>()
    .ByOrganism<"mouse">()
    .Reviewed()
```

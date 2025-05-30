namespace UniProtProvider.DesignTime

open System.Reflection
open FSharp.Core.CompilerServices
open Client
open ProviderImplementation.ProvidedTypes
open UniProtProvider.DesignTime.InnerTypes


[<TypeProvider>]
type ById (config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces (
        config,
        assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")]
    )
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    let retrieveById = ProvidedTypeDefinition(
        asm, 
        ns, 
        "ById", 
        Some typeof<obj>, 
        hideObjectMethods=true)
    do retrieveById.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter(
        "UniProtKBId",
        typeof<string>
    )
    do retrieveById.DefineStaticParameters( [parameter], fun typeName args -> 
        let id = unbox<string> args.[0]
        let result = getProteinById id |> Async.RunSynchronously
        let value = result.primaryAccession
        let name = 
            if result.proteinDescription.recommendedName.IsSome then
                result.proteinDescription.recommendedName.Value.fullName.value
            else if result.proteinDescription.submissionNames.IsSome then
                result.proteinDescription.submissionNames.Value[0].fullName.value
            else if result.proteinDescription.alternativeNames.IsSome then
                result.proteinDescription.alternativeNames.Value[0].fullName.value
            else
                ""
        let propertyName = name + " (" + result.uniProtkbId + ")"
        let prot = ProvidedTypeDefinition(
            typeName,
            Some typeof<obj>,
            hideObjectMethods=true)

        prot.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))

        let p = ProvidedProperty(
            propertyName = propertyName,
            propertyType = typeof<Protein>,
            getterCode = (fun _ -> <@@ getProteinById value |> Async.RunSynchronously @@>))
        prot.AddMember p

        retrieveById.AddMember prot
        prot
    )
    let helpText =
        """<summary>Typed representation of the protein entry of the UniProtKB database.</summary>
            <param name="UniProtKBId">Protein entry accession.</param>
            <returns>Typed representation of the protein entry.</returns>"""
    do retrieveById.AddXmlDoc helpText
    do this.AddNamespace(ns, [retrieveById])


[<TypeProvider>]
type ByTaxonId (config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces (
        config,
        assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")]
    )
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    let retrieveById = ProvidedTypeDefinition(
        asm,
        ns,
        "ByTaxonId",
        Some typeof<obj>,
        hideObjectMethods=true)
    do retrieveById.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter(
        "TaxonId",
        typeof<int>
    )
    do retrieveById.DefineStaticParameters([parameter], fun typeName args ->
        let id = unbox<int> args.[0]
        let result = getOrganismById id |> Async.RunSynchronously
        let value = result.taxonId
        let name =  result.scientificName + " (" + string value + ")"
        let prot = ProvidedTypeDefinition(
            typeName,
            Some typeof<obj>,
            hideObjectMethods=true)

        prot.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))

        let p = ProvidedProperty(
            propertyName = name,
            propertyType = typeof<Taxonomy>,
            getterCode = (fun _ -> <@@ getOrganismById value |> Async.RunSynchronously @@>)
        )
        prot.AddMember p

        retrieveById.AddMember prot
        prot
    )
    let helpText =
        """<summary>Typed representation of the taxonomy entry of the UniProtKB database.</summary>
            <param name="TaxonId">Unique identifier for the taxonomy entry.</param>
            <returns>Typed representation of the taxonomy entry.</returns>"""
    do retrieveById.AddXmlDoc helpText
    do this.AddNamespace(ns, [retrieveById])

[<TypeProvider>]
type ByKeyWord (config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces (
        config,
        assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")]
    )
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)

    let retrieveByKeyWord = ProvidedTypeDefinition(
        asm,
        ns,
        "ByKeyWord", 
        Some typeof<obj>, 
        hideObjectMethods=true
    )
    do retrieveByKeyWord.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter(
        "KeyWord",
        typeof<string>
    )
    do retrieveByKeyWord.DefineStaticParameters([parameter], fun typeName args ->
        let prot = ProvidedTypeDefinition(
            typeName,
            Some typeof<obj>,
            hideObjectMethods=true
        )
        prot.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))

        let keyword = unbox<string> args.[0]
        let param = Params.Create(keyword, Entity.Protein)
        let result = getProteinsByKeyWord param |> Async.RunSynchronously

        if result.results.Length = 0 then
            // no matching entries found, generate suggested queries
            if result.suggestions.IsSome && result.suggestions.Value.Length <> 0 then
                prot.AddMembersDelayed(getSuggestions result.suggestions.Value prot)
        else
            prot.AddMembersDelayed(getProteinProperties result.results)
            prot.AddMemberDelayed(getByOrganism param prot)
            prot.AddMemberDelayed(getReviewed param prot)
            prot.AddMemberDelayed(getByProteinExistence param prot)

            let newCursor = getCursor param |> Async.RunSynchronously
            if newCursor.IsSome then
                let nextParam = {param with cursor = Some newCursor.Value}
                prot.AddMemberDelayed(getNext nextParam prot)

        retrieveByKeyWord.AddMember prot
        prot
    )
    let helpText =
        """<summary>Typed representation of the protein entries of the UniProtKB database.</summary>
            <param name="KeyWord">Protein search term.</param>
            <returns>All entries associated with the search term in a paginated list of entries.</returns>"""
    do retrieveByKeyWord.AddXmlDoc helpText
    do this.AddNamespace(ns, [retrieveByKeyWord])

[<TypeProvider>]
type ByOrganism (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (
        config,
        assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")]
    )
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)
    let retrieveByOrganism = ProvidedTypeDefinition(
        asm,
        ns,
        "ByOrganism",
        Some typeof<obj>,
        hideObjectMethods=true)
    do retrieveByOrganism.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter(
        "OrganismName",
        typeof<string>
    )
    do retrieveByOrganism.DefineStaticParameters([parameter], fun typeName args ->
        let taxon = ProvidedTypeDefinition(
            typeName,
            Some typeof<obj>, 
            hideObjectMethods=true
        )
        taxon.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))

        let organismName = unbox<string> args.[0]
        let param = Params.Create(organismName, Entity.Taxonomy)
        let result = getOrganismsByKeyWord param |> Async.RunSynchronously

        taxon.AddMembersDelayed(getOrganismResults result.results taxon)
        let newCursor = getCursor param |> Async.RunSynchronously
        if newCursor.IsSome then
            let nextParam = {param with cursor = Some newCursor.Value}
            taxon.AddMemberDelayed(getNext nextParam taxon)

        retrieveByOrganism.AddMember taxon
        taxon
    )
    let helpText =
        """<summary>Typed representation of the taxonomy entries of the UniProtKB database.</summary>
            <param name="OrganismName">Taxonomy search term.</param>
            <returns>All entries associated with the search term in a paginated list of entries.</returns>"""
    do retrieveByOrganism.AddXmlDoc helpText
    do this.AddNamespace(ns, [retrieveByOrganism])
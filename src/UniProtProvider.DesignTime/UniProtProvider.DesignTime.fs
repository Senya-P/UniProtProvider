namespace UniProtProvider.DesignTime

open System.Reflection
open FSharp.Core.CompilerServices
open Client.UniProtClient
open Types
open ProviderImplementation.ProvidedTypes
open UniProtProvider.DesignTime.InnerTypes


[<TypeProvider>]
type ById (config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")])
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    let retrieveById = 
        ProvidedTypeDefinition(
        asm, 
        ns, 
        "ById", 
        Some typeof<obj>, 
        hideObjectMethods=true)
    do retrieveById.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter("UniProtKBId", typeof<string>)
    do retrieveById.DefineStaticParameters( [parameter], fun typeName args -> 
        let id = (unbox<string> args.[0])
        let result = getProteinById id
        let value = result.uniProtkbId
        let name =  System.String.Concat(result.proteinDescription.recommendedName.Value.fullName.value, " (", result.uniProtkbId, ")")
        let prot = 
            ProvidedTypeDefinition(typeName, 
            Some typeof<obj>,
            hideObjectMethods=true)
        prot.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
        let p =
            ProvidedProperty(propertyName = name,
            propertyType = typeof<Prot>,
            getterCode = (fun _ -> <@@ getProteinById value @@>))
        prot.AddMember p
        retrieveById.AddMember prot
        prot
    )

    do this.AddNamespace(ns, [retrieveById])

[<TypeProvider>]
type ByKeyWord (config : TypeProviderConfig) as this = 
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")])
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)

    let retrieveByKeyWord = 
        ProvidedTypeDefinition(
        asm,
        ns,
        "ByKeyWord", 
        Some typeof<obj>, 
        hideObjectMethods=true)
    do retrieveByKeyWord.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter("KeyWord", typeof<string>)
    do retrieveByKeyWord.DefineStaticParameters( [parameter], fun typeName args ->
        let prot = 
            ProvidedTypeDefinition(typeName, 
            Some typeof<obj>, 
            hideObjectMethods=true)
        prot.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))

        let keyword = unbox<string> args.[0]
        let param = Params(keyword)
        let result = getProteinsByKeyWord param

        if result.results.Length = 0 then
            if result.suggestions.IsSome && result.suggestions.Value.Length <> 0 then
                addSuggestions result.suggestions.Value prot
        else
            prot.AddMembersDelayed(getProperties result.results)
            prot.AddMemberDelayed(addByOrganism param prot)
            let cursor = getCursor param |> Async.RunSynchronously
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                prot.AddMemberDelayed(addNext nextParam prot)

        retrieveByKeyWord.AddMember prot
        prot
    )

    do this.AddNamespace(ns, [retrieveByKeyWord])
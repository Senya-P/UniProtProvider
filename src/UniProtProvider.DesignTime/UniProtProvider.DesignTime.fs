namespace UniProtProvider.DesignTime

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open FSharp.Quotations
open FSharp.Core.CompilerServices
open UniProtProvider.RunTime
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Reflection
open System.Text.RegularExpressions


// Put any utility helpers here
[<AutoOpen>]
module internal Helpers =
    let x = 1

[<TypeProvider>]
type BasicErasingProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")], addDefaultProbingLocation=true)

    let ns = "MyNamespace"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    let createTypes () =
        let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)

        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[0]):string) :> obj @@>)
        myType.AddMember(ctor2)

        let innerState = ProvidedProperty("InnerState", typeof<string>, getterCode = fun args -> <@@ (%%(args.[0]) :> obj) :?> string @@>)
        myType.AddMember(innerState)

        let meth = ProvidedMethod("StaticMethod", [], typeof<DataSource>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<DataSource>)))
        myType.AddMember(meth)

        let nameOf =
            let param = ProvidedParameter("p", typeof<Expr<int>>)
            param.AddCustomAttribute {
                new CustomAttributeData() with
                    member __.Constructor = typeof<ReflectedDefinitionAttribute>.GetConstructor([||])
                    member __.ConstructorArguments = [||] :> _
                    member __.NamedArguments = [||] :> _
            }
            ProvidedMethod("NameOf", [ param ], typeof<string>, isStatic = true, invokeCode = fun args ->
                <@@
                    match (%%args.[0]) : Expr<int> with
                    | Microsoft.FSharp.Quotations.Patterns.ValueWithName (_, _, n) -> n
                    | e -> failwithf "Invalid quotation argument (expected ValueWithName): %A" e
                @@>)
        myType.AddMember(nameOf)

        [myType]

    do
        this.AddNamespace(ns, createTypes())

[<TypeProvider>]
type BasicGenerativeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")])

    let ns = "MyNamespace"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    let createType typeName (count:int) =
        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased=false)

        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[1]):string) :> obj @@>)
        myType.AddMember(ctor2)

        for i in 1 .. count do 
            let prop = ProvidedProperty("Property" + string i, typeof<int>, getterCode = fun args -> <@@ i @@>)
            myType.AddMember(prop)

        let meth = ProvidedMethod("StaticMethod", [], typeof<DataSource>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<DataSource>)))
        myType.AddMember(meth)
        asm.AddTypes [ myType ]

        myType

    let myParamType = 
        let t = ProvidedTypeDefinition(asm, ns, "GenerativeProvider", Some typeof<obj>, isErased=false)
        t.DefineStaticParameters( [ProvidedStaticParameter("Count", typeof<int>)], fun typeName args -> createType typeName (unbox<int> args.[0]))
        t
    do
        this.AddNamespace(ns, [myParamType])

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
        //isErased=false, 
        hideObjectMethods=true)
    do retrieveById.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter("UniProtKBId", typeof<string>)
    do retrieveById.DefineStaticParameters( [parameter], fun typeName args -> 
        let id = (unbox<string> args.[0])
        let result = TypeGenerator.genTypeById id
        let value = result.uniProtkbId
        let name = result.proteinDescription.recommendedName.Value.fullName.value
        let prot = 
            ProvidedTypeDefinition(typeName, 
            Some typeof<obj>,
            //isErased=false, 
            hideObjectMethods=true)
        prot.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
        let p =
            ProvidedProperty(propertyName = name,
            propertyType = typeof<Prot>,
            //isStatic = true,
            getterCode = (fun _ -> <@@ TypeGenerator.genTypeById value @@>))
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

    let mutable count = 0
    let nextNumber() = count <- count + 1; count // serves to generate unique type names

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
        let query = unbox<string> args.[0]
        let param = TypeGenerator.Params(query)
        let result = TypeGenerator.genTypesByKeyWord param
        let getProps (props : array<ProtIncomplete>) () =
            [for i in props do
                let name = i.proteinDescription.recommendedName.Value.fullName.value
                let value = i.uniProtkbId
                let p =
                    ProvidedProperty(propertyName = name,
                    propertyType = typeof<Prot>,
                    getterCode = (fun args -> <@@ TypeGenerator.genTypeById value @@>))
                p]


        let rec addByOrganism (param : TypeGenerator.Params) () = 
            let byOrganism = ProvidedMethod("ByOrganism", [], typeof<obj>)
            byOrganism.DefineStaticParameters([ProvidedStaticParameter("Name", typeof<string>)], fun methName args ->
                let name = args.[0] :?> string
                param.organism <- name

                let t = ProvidedTypeDefinition("InnerType" + string(nextNumber()), Some typeof<obj>, true)
                let res = TypeGenerator.genTypesByKeyWord param
                t.AddMembersDelayed(getProps res.results)
                t.AddMemberDelayed(addByOrganism param)

                let cursor = TypeGenerator.getCursor param |> Async.RunSynchronously
                if cursor.IsSome then
                    let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                    t.AddMemberDelayed(addNext nextParam)
                prot.AddMember(t)

                let m = ProvidedMethod(methName, [], t, invokeCode = fun _ -> <@@ obj() @@>)
                prot.AddMember(m)
                m
            )
            byOrganism

        and addNext (param: TypeGenerator.Params) () =

            let result = TypeGenerator.genTypesByKeyWord param
            let next = 
                ProvidedTypeDefinition("InnerType" + string(nextNumber()),
                Some typeof<obj>,
                hideObjectMethods=true)

            next.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
            next.AddMembersDelayed (getProps result.results)
            next.AddMemberDelayed (addByOrganism param)

            prot.AddMember next

            let cursor = TypeGenerator.getCursor param |> Async.RunSynchronously
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                next.AddMemberDelayed(addNext nextParam)

            let p =
                ProvidedProperty(propertyName="More...",
                propertyType = next,
                getterCode = (fun _ -> <@@ obj() @@>))
            p

        and  byTaxonomy () = 
            let byTaxonomy = ProvidedMethod("ByTaxonName", [], typeof<obj>)
            byTaxonomy

        let addSuggestions (sug : array<Suggestion>) =
            for i in sug do
                let query = i.query.Value
                let param = TypeGenerator.Params(query)
                let result = TypeGenerator.genTypesByKeyWord param
                let suggested = 
                    ProvidedTypeDefinition("InnerType" + string(nextNumber()),
                    Some typeof<obj>,
                    hideObjectMethods=true)

                suggested.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
                suggested.AddMembersDelayed (getProps result.results)

                let param = TypeGenerator.Params(query)
                suggested.AddMemberDelayed(addByOrganism param)

                let cursor = TypeGenerator.getCursor param |> Async.RunSynchronously
                if cursor.IsSome then
                    let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                    suggested.AddMemberDelayed(addNext nextParam)

                prot.AddMember suggested
                let p =
                    ProvidedProperty(propertyName=query,
                    propertyType = suggested,
                    getterCode = (fun _ -> <@@ obj() @@>))
                prot.AddMember p

        if result.results.Length = 0 then
            if result.suggestions.IsSome && result.suggestions.Value.Length <> 0 then
                addSuggestions result.suggestions.Value
        else
            prot.AddMembersDelayed(getProps result.results)
            prot.AddMemberDelayed(addByOrganism param)
            let cursor = TypeGenerator.getCursor param |> Async.RunSynchronously
            if cursor.IsSome then
                let nextParam = param.Clone() in nextParam.cursor <- cursor.Value
                prot.AddMemberDelayed(addNext nextParam)

        retrieveByKeyWord.AddMember prot
        prot
    )

    do this.AddNamespace(ns, [retrieveByKeyWord])
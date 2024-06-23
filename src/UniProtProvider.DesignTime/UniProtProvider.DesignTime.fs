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
type UniProtKBProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")])
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    (*
    let genStaticProps (props : array<ProtIncomplete>) = 
        let staticProps = [
            for i in props do
                let valueOfTheProperty = string i.uniProtkbId
                let p =
                    ProvidedProperty(propertyName = valueOfTheProperty,
                    propertyType = typeof<string>,
                    isStatic = true,
                    getterCode= (fun args -> <@@ valueOfTheProperty @@>))
                p
        ]
        staticProps
    *)
    (*
    let addProps (props : array<ProtIncomplete>) (nestedType : ProvidedTypeDefinition) =
        for i in props do
            let valueOfTheProperty = i.uniProtkbId
            let p =
                ProvidedProperty(propertyName = valueOfTheProperty,
                propertyType = typeof<Prot>,
                isStatic = true,
                getterCode= (fun args -> <@@ TypeGenerator.genTypeById valueOfTheProperty @@>))
            nestedType.AddMember p

    let prot = ProvidedTypeDefinition("Prot", Some typeof<obj>, isErased=false)
    addProps (TypeGenerator.genTypesByKeyWord "human") prot
    uniProtKB.AddMember prot
    *)
    (*
    let retrieveByKeyWord = ProvidedMethod("ByKeyWord", 
        [ProvidedParameter("KeyWord", typeof<string>)], 
        typeof<array<ProtIncomplete>>,
        isStatic=true,
        invokeCode = (fun args -> <@@ TypeGenerator.genTypesByKeyWord  (%%(args.[0]):string) @@>))
        
    retrieveByKeyWord.DefineStaticParameters( [ProvidedStaticParameter("Count", typeof<string>)], fun methodName args -> 
        if unbox<string> args.[0] = "test" then
            let m = ProvidedMethod(methodName, [], typeof<int>, (fun _ -> <@@ 1 @@>), isStatic=true)
            uniProtKB.AddMember m
            m
        else
            let m = ProvidedMethod(methodName, [], typeof<string>, (fun _ -> <@@ "result" @@>), isStatic=true)
            uniProtKB.AddMember m
            m 
        
        )
    *)
    (*
    let retrieveByKeyWord = ProvidedMethod("ByKeyWord", 
        [], 
        typeof<obj>,
        invokeCode = (fun _ -> <@@ obj() @@>),
        isStatic=true)
        *)
    let buildType() =
        let asm = ProvidedAssembly()
        let uniProtKB = 
            ProvidedTypeDefinition(asm, 
            ns, 
            "UniProtKBProvider", 
            Some typeof<obj>, 
            isErased=false)
        
        let retrieveById = ProvidedMethod("ById", 
            [ProvidedParameter("UniProtKBId", typeof<string>)], 
            typeof<Prot>, 
            isStatic=true,
            invokeCode = (fun args -> <@@ TypeGenerator.genTypeById  (%%(args.[0]):string)  @@>))
        uniProtKB.AddMember(retrieveById)
        
        let retrieveByKeyWord = 
            ProvidedTypeDefinition("ByKeyWord", 
            Some typeof<obj>, 
            isErased=false, 
            hideObjectMethods=true)
        let parameter = ProvidedStaticParameter("KeyWord", typeof<string>)
        retrieveByKeyWord.DefineStaticParameters( [parameter], fun typeName args ->
            let prot = 
                ProvidedTypeDefinition(typeName, 
                Some typeof<obj>, 
                isErased=false, 
                hideObjectMethods=true)
            let result = TypeGenerator.genTypesByKeyWord (unbox<string> args.[0])
            let addProps (props : array<ProtIncomplete>) (nestedType : ProvidedTypeDefinition) =
                for i in props do
                    let name = i.primaryAccession
                    let value = i.uniProtkbId
                    let p =
                        ProvidedProperty(propertyName = name,
                        propertyType = typeof<Prot>,
                        isStatic = true,
                        getterCode = (fun args -> <@@ TypeGenerator.genTypeById value @@>))
                    nestedType.AddMember p

            addProps result.results prot
            uniProtKB.AddMember prot
            prot
        )
        uniProtKB.AddMember(retrieveByKeyWord)
        asm.AddTypes [ uniProtKB ]
        uniProtKB
    do this.AddNamespace(ns, [buildType()])

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
        //isErased=false, 
        hideObjectMethods=true)
    do retrieveByKeyWord.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
    let parameter = ProvidedStaticParameter("KeyWord", typeof<string>)
    do retrieveByKeyWord.DefineStaticParameters( [parameter], fun typeName args ->
        let prot = 
            ProvidedTypeDefinition(typeName, 
            Some typeof<obj>, 
            //isErased=false, 
            hideObjectMethods=true)
        prot.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
        let result = TypeGenerator.genTypesByKeyWord (unbox<string> args.[0])

        let addProps (props : array<ProtIncomplete>) (nestedType : ProvidedTypeDefinition) =
            for i in props do
                let name = i.proteinDescription.recommendedName.Value.fullName.value
                let value = i.uniProtkbId
                let p =
                    ProvidedProperty(propertyName = name,
                    propertyType = typeof<Prot>,
                    //isStatic = true,
                    getterCode = (fun args -> <@@ TypeGenerator.genTypeById value @@>))
                nestedType.AddMember p

        let addSuggestions (sug : array<Suggestion>) (nestedType : ProvidedTypeDefinition) =
            for i in sug do
                let query = i.query.Value
                let result = TypeGenerator.genTypesByKeyWord query
                let suggested = 
                    ProvidedTypeDefinition(query, 
                    Some typeof<obj>, 
                    //isErased=false, 
                    hideObjectMethods=true)
                suggested.AddMember(ProvidedConstructor([], fun _ -> <@@ obj() @@>))
                let getProps () =
                    [ for i in result.results ->
                        let name = i.proteinDescription.recommendedName.Value.fullName.value
                        let value = i.uniProtkbId
                        let p =
                            ProvidedProperty(propertyName = name,
                            propertyType = typeof<Prot>,
                            getterCode = (fun _ -> <@@ TypeGenerator.genTypeById value @@>))
                        p ]
                suggested.AddMembersDelayed (getProps)
                let p =
                    ProvidedProperty(propertyName = query,
                    propertyType = suggested,
                    getterCode = (fun _ -> <@@ obj() @@>))
                nestedType.AddMember suggested
                nestedType.AddMember p
                

            (*
                let query = i.query.Value
                let p =
                    ProvidedProperty(propertyName = query,
                    propertyType = typeof<IncompleteResult>,
                    //isStatic = true,
                    getterCode = (fun args -> <@@ TypeGenerator.genTypesByKeyWord query @@>))
                nestedType.AddMember p
            *)

        if result.results.Length = 0 then
            addSuggestions result.suggestions.Value prot
        else
            addProps result.results prot

        let byOrganism = ProvidedMethod("ByOrganism", [], typeof<obj>)
        byOrganism.DefineStaticParameters([ProvidedStaticParameter("Name", typeof<string>)], fun methName args ->
            let s = args.[0] :?> string

            let t = ProvidedTypeDefinition("Hidden" + string (nextNumber()), Some typeof<obj>)
            t.AddMember(ProvidedProperty(s, typeof<string>, getterCode=fun _ -> <@@ s @@>))
            prot.AddMember(t)

            let m = ProvidedMethod(methName, [], t, invokeCode = fun _ -> <@@ obj() @@>)
            prot.AddMember(m)
            m
        )
        prot.AddMember(byOrganism)

        retrieveByKeyWord.AddMember prot
        prot
    )

    do this.AddNamespace(ns, [retrieveByKeyWord])
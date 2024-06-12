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
type StressErasingProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, addDefaultProbingLocation=true)

    let ns = "StressProvider"
    let asm = Assembly.GetExecutingAssembly()

    let newProperty t name getter isStatic = ProvidedProperty(name, t, getter, isStatic = isStatic)
    let newStaticProperty t name getter = newProperty t name (fun _ -> getter) true
    let newInstanceProperty t name getter = newProperty t name (fun _ -> getter) false
    let addStaticProperty t name getter (typ:ProvidedTypeDefinition) = typ.AddMember (newStaticProperty t name getter); typ
    let addInstanceProperty t name getter (typ:ProvidedTypeDefinition) = typ.AddMember (newInstanceProperty t name getter); typ

    let provider = ProvidedTypeDefinition(asm, ns, "Provider", Some typeof<obj>, hideObjectMethods = true)
    let tags = ProvidedTypeDefinition(asm, ns, "Tags", Some typeof<obj>, hideObjectMethods = true)           
    do [1..2000] |> Seq.iter (fun i -> addInstanceProperty typeof<int> (sprintf "Tag%d" i) <@@ i @@> tags |> ignore)

    do provider.DefineStaticParameters([ProvidedStaticParameter("Host", typeof<string>)], fun name args ->
        let provided = ProvidedTypeDefinition(asm, ns, name, Some typeof<obj>, hideObjectMethods = true)
        addStaticProperty tags "Tags" <@@ obj() @@> provided |> ignore
        provided
    )

    // An example provider with one _optional_ static parameter
    let provider2 = ProvidedTypeDefinition(asm, ns, "Provider2", Some typeof<obj>, hideObjectMethods = true)
    do provider2.DefineStaticParameters([ProvidedStaticParameter("Host", typeof<string>, "default")], fun name args ->
        let provided = 
            let srv = args.[0] :?> string
            let prop = ProvidedProperty("Server", typeof<Server>, (fun _ -> <@@ Server(srv) @@>), isStatic = true)
            let provided = ProvidedTypeDefinition(asm, ns, name, Some typeof<obj>, hideObjectMethods = true)
            provided.AddMember prop
            addStaticProperty tags "Tags" <@@ obj() @@> provided |> ignore
            provided

        provided
    )

    let provider3 = ProvidedTypeDefinition(asm, ns, "Provider3", Some typeof<obj>, hideObjectMethods = true)

    do provider3.DefineStaticParameters([ProvidedStaticParameter("Host", typeof<string>)], fun name _ ->
        let provided = ProvidedTypeDefinition(asm, ns, name, Some typeof<obj>, hideObjectMethods = true)

        let fn = ProvidedMethod("Test", [ ProvidedParameter("disp", typeof<IDisposable>) ], typeof<string>, fun [ arg ] ->
            <@@
                use __ = (%%arg : IDisposable)
                let mutable res = ""

                try 
                    try
                        System.Console.WriteLine() // test calling a method with void return type
                        failwith "This will throw anyway, don't mind it."

                        res <- "[-] Should not get here."
                    finally
                        res <- "[+] Caught try-finally, nice."

                        try
                            failwith "It failed again."

                            res <- "[-] Should not get here."
                        with
                        | _ ->
                            res <- "[+] Caught try-with, nice."

                        try
                            res <- "[?] Gonna go to finally without throwing..."
                        finally
                            res <- "[+] Yup, it worked totally."
                    res
                with _ -> 
                    res
            @@>
        , isStatic = true)

        provided.AddMember fn
        provided
    )

    do this.AddNamespace(ns, [provider; provider2; provider3; tags])

[<TypeProvider>]
type StressGenerativeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config)

    let ns = "StressProvider"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<SomeRuntimeHelper>.Assembly.GetName().Name = asm.GetName().Name)  

    let createType typeName (count:int) =
        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased=false)

        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[1]):string) :> obj @@>)
        myType.AddMember(ctor2)

        for i in 1 .. count do 
            let prop = ProvidedProperty("PropertyWithTryCatch" + string i, typeof<int>, getterCode = fun args -> <@@ try i with _ -> i+1 @@>)
            myType.AddMember(prop)

        for i in 1 .. count do 
            let prop = ProvidedProperty("PropertyWithTryFinally" + string i, typeof<int>, getterCode = fun args -> <@@ try i finally ignore i @@>)
            myType.AddMember(prop)

        let meth = ProvidedMethod("StaticMethod", [], typeof<SomeRuntimeHelper>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<SomeRuntimeHelper>)))
        myType.AddMember(meth)
        asm.AddTypes [ myType ]

        myType

    let provider = 
        let t = ProvidedTypeDefinition(asm, ns, "GenerativeProvider", Some typeof<obj>, isErased=false)
        t.DefineStaticParameters( [ProvidedStaticParameter("Count", typeof<int>)], fun typeName args -> createType typeName (unbox<int> args.[0]))
        t

    let provider3 = ProvidedTypeDefinition(asm, ns, "GenerativeProvider3", Some typeof<obj>, hideObjectMethods = true)

    do provider3.DefineStaticParameters([ProvidedStaticParameter("Host", typeof<string>)], fun name _ ->
        let provided = ProvidedTypeDefinition(asm, ns, name, Some typeof<obj>, hideObjectMethods = true)

        let fn = ProvidedMethod("Test", [ ProvidedParameter("disp", typeof<IDisposable>) ], typeof<string>, fun [ arg ] ->
            <@@
                use __ = (%%arg : IDisposable)
                let mutable res = ""

                try 
                    try
                        System.Console.WriteLine() // test calling a method with void return type
                        failwith "This will throw anyway, don't mind it."

                        res <- "[-] Should not get here."
                    finally
                        res <- "[+] Caught try-finally, nice."

                        try
                            failwith "It failed again."

                            res <- "[-] Should not get here."
                        with
                        | _ ->
                            res <- "[+] Caught try-with, nice."

                        try
                            res <- "[?] Gonna go to finally without throwing..."
                        finally
                            res <- "[+] Yup, it worked totally."
                    res
                with _ -> 
                    res
            @@>
        , isStatic = true)

        provided.AddMember fn
        provided
    )

    do
        this.AddNamespace(ns, [provider; provider3])

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
            isErased=true)
        
        let retrieveById = ProvidedMethod("ById", 
            [ProvidedParameter("UniProtKBId", typeof<string>)], 
            typeof<Prot>, 
            isStatic=true,
            invokeCode = (fun args -> <@@ TypeGenerator.genTypeById  (%%(args.[0]):string)  @@>))
        uniProtKB.AddMember(retrieveById)
        
        let retrieveByKeyWord = 
            ProvidedTypeDefinition("ByKeyWord", 
            Some typeof<obj>, 
            isErased=true, 
            hideObjectMethods=true)
        let parameter = ProvidedStaticParameter("KeyWord", typeof<string>)
        retrieveByKeyWord.DefineStaticParameters( [parameter], fun typeName args ->
            let prot = 
                ProvidedTypeDefinition(typeName, 
                Some typeof<obj>, 
                isErased=true, 
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

            addProps result prot
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
        isErased=false, 
        hideObjectMethods=true)
    let parameter = ProvidedStaticParameter("UniProtKBId", typeof<string>)
    do retrieveById.DefineStaticParameters( [parameter], fun typeName args -> 
        let result = TypeGenerator.genTypeById (unbox<string> args.[0])
        let prot = 
            ProvidedTypeDefinition(typeName, 
            Some typeof<obj>,
            isErased=false, 
            hideObjectMethods=true)
        let p =
            ProvidedProperty(propertyName = result.primaryAccession,
            propertyType = typeof<Prot>,
            isStatic = true,
            getterCode = (fun args -> <@@ result @@>))
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
        isErased=false, 
        hideObjectMethods=true)
    let parameter = ProvidedStaticParameter("KeyWord", typeof<string>)
    do retrieveByKeyWord.DefineStaticParameters( [parameter], fun typeName args ->
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

        addProps result prot
        retrieveByKeyWord.AddMember prot
        prot
    )

    do this.AddNamespace(ns, [retrieveByKeyWord])
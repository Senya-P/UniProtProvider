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

(*
[<TypeProvider>]
type UniParcProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")])
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    let buildType  () =
        let asm = ProvidedAssembly()
        let uniParc = ProvidedTypeDefinition(asm, ns, "UniParcProvider", Some typeof<obj>, isErased=false)
        (*
        let ctor = ProvidedConstructor([ProvidedParameter("uniParcId", typeof<string>)], invokeCode = fun args -> <@@ TypeGenerator.genType (%%(args.[0]):string) :> obj @@>)
        uniParc.AddMember(ctor)
        *)
        (*
        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "" @@>)
        uniParc.AddMember(ctor)
        *)
        let retrieveById = ProvidedMethod("ById", 
            [ProvidedParameter("UniParcId", typeof<string>)], 
            typeof<ProtSeq>, 
            isStatic=true,
            invokeCode = (fun args -> <@@ TypeGenerator.genType (%%(args.[0]):string)  @@>))
        uniParc.AddMember(retrieveById)

        (*
        let getPropertyNames (s : System.Type) =
            Seq.map (fun (t:System.Reflection.PropertyInfo) -> t.Name) (s.GetProperties())

        let names = getPropertyNames (typeof<ProtSeq>)
        for i in names do
            let prop = ProvidedProperty(i, typeof<string>, getterCode = fun args -> <@@ i @@>)
        // actually memeber of returned type
            uniParc.AddMember prop
        *)
        asm.AddTypes [ uniParc ]
        uniParc
    (*
    let providedType = ProvidedTypeDefinition(asm, ns, "UniParcProvider", Some typeof<obj>, isErased=false)
    let assemblyParam = ProvidedStaticParameter("ID", typeof<string>, parameterDefaultValue = "")
    do providedType.DefineStaticParameters([assemblyParam], fun typeName args -> buildType typeName )
    *)
    //let assemblyProvidedType = ProvidedTypeDefinition(asm, ns, "UniParcProvider", Some typeof<obj>, isErased=false)
    //let assemblyParam = ProvidedStaticParameter("ID", typeof<string>, parameterDefaultValue = "")
    do
        this.AddNamespace(ns, [buildType()])
*)
[<TypeProvider>]
type UniProtKBProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("UniProtProvider.DesignTime", "UniProtProvider.Runtime")])
    let ns = "UniProtProvider"
    let asm = Assembly.GetExecutingAssembly()
    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)  

    let buildType  () =
        let asm = ProvidedAssembly()
        let uniProtKB = ProvidedTypeDefinition(asm, ns, "UniProtKBProvider", Some typeof<obj>, isErased=false)
        let retrieveById = ProvidedMethod("ById", 
            [ProvidedParameter("UniProtKBId", typeof<string>)], 
            typeof<Prot>, 
            isStatic=true,
            invokeCode = (fun args -> <@@ TypeGenerator.genType (%%(args.[0]):string)  @@>))
        uniProtKB.AddMember(retrieveById)
        asm.AddTypes [ uniProtKB ]
        uniProtKB
    do
        this.AddNamespace(ns, [buildType()])


module UniProtProviderTests
open MyNamespace
open UniProtProvider
open NUnit.Framework
type Assert() =
    static member AreEqual(a, b) = if a <> b then failwith "oops"

[<Test>]
let ``Default constructor should create instance`` () =
    Assert.AreEqual("My internal state", MyType().InnerState)

[<Test>]
let ``Constructor with parameter should create instance`` () =
    Assert.AreEqual("override", MyType("override").InnerState)

[<Test>]
let ``Method with ReflectedDefinition parameter should get its name`` () =
    let myValue = 2
    Assert.AreEqual("myValue", MyType.NameOf(myValue))

type Generative2 = MyNamespace.GenerativeProvider<2>
type Generative4 = MyNamespace.GenerativeProvider<4>

let ds = MyType.StaticMethod()

let dsg = Generative2.StaticMethod()

[<Test>]
let ``Can access properties of generative provider 2`` () =
    let obj = Generative2("Inner")
    Assert.AreEqual(obj.Property1, 1)
    Assert.AreEqual(obj.Property2, 2)

[<Test>]
let ``Can access properties of generative provider 4`` () =
    let obj: Generative4 = Generative4()
    Assert.AreEqual(obj.Property1, 1)
    Assert.AreEqual(obj.Property2, 2)
    Assert.AreEqual(obj.Property3, 3)
    Assert.AreEqual(obj.Property4, 4)

// UNIPROT

type UniProtKB = UniProtKBProvider
//type Human = UniProtKB.ByKeyWord<"Human">

type Human1 = UniProtProvider.ById<"P42694">

type Human2 = UniProtProvider.ByKeyWord<"Human">

[<Test>]
let ``ById and ByKeyWord return the same`` () =
    let genes1 = Human1.P42694.genes
    let genes2 = Human2.P42694.genes
    let seq1 = Human1.P42694.sequence
    let seq2 = Human2.P42694.sequence
    Assert.AreEqual(genes1, genes2)
    Assert.AreEqual(seq1, seq2)

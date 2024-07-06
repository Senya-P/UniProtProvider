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

//type UniProtKB = UniProtKBProvider
//type Human = UniProtKB.ByKeyWord<"Human">

let human1 = UniProtProvider.ById<"P42694">()

let human2 = UniProtProvider.ByKeyWord<"Human">()

let keratin = UniProtProvider.ByKeyWord<"keratin">()

let insul = UniProtProvider.ByKeyWord<"insul">()
// let q = UniProtProvider.ByKeyWord<"qqqqq">()
//let insul_ = insul.indus.ByOrganism<"human">()

let sqwq = UniProtProvider.ByKeyWord<"sqwq">()

let sqwq_ = sqwq.sqwf.``Sulfoacetaldehyde reductase``

let keratin_ = keratin.ByOrganism<"human">().``Keratin-like protein KRT222``

let insul_ = insul.insult.ByOrganism<"mouse">().``C-C chemokine receptor type 6``

[<Test>]
let ``ById and ByKeyWord return the same`` () =
    let genes1 = human1.``Probable helicase with zinc finger domain``.genes.Value
    let genes2 = human2.``Probable helicase with zinc finger domain``.genes.Value
    let seq1 = human1.``Probable helicase with zinc finger domain``.sequence.Value
    let seq2 = human2.``Probable helicase with zinc finger domain``.sequence.Value
    Assert.AreEqual(genes1, genes2)
    Assert.AreEqual(seq1, seq2)

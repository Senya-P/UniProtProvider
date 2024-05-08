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
let obj = UniProtKB.ById("P68452")
let obj2 = UniProtKB.ByKeyWord("human")[3]
let md5 = obj.sequence.md5

let w = obj.comments

for i in w do 
    printf "%s" i.commentType 
    printf "%A" i.texts

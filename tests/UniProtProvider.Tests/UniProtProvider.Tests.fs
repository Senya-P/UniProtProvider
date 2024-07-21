module UniProtProviderTests
open UniProtProvider
open NUnit.Framework

type Assert() =
    static member AreEqual(a, b) = if a <> b then failwith "oops"

let human1 = UniProtProvider.ById<"P42694">()

let human2 = UniProtProvider.ByKeyWord<"Human">()

let h_ = human2

let keratin = UniProtProvider.ByKeyWord<"keratin">()

let insul = UniProtProvider.ByKeyWord<"insul">()

//let insul_ = insul.indus.ByOrganism<"human">()

let sqwq = UniProtProvider.ByKeyWord<"sqwq">()

let sqwq_ = sqwq.sqwf.``Sulfoacetaldehyde reductase``

let keratin_ = keratin.ByOrganism<"human">().``Keratin, type I cytoskeletal 14``.genes

let insul_ = insul.insult.ByOrganism<"mou">().ByOrganism<"mouse">().``C-C chemokine receptor type 6``



[<Test>]
let ``ById and ByKeyWord return the same`` () =
    let genes1 = human1.``Probable helicase with zinc finger domain``.genes.Value
    let genes2 = human2.``Probable helicase with zinc finger domain``.genes.Value
    let seq1 = human1.``Probable helicase with zinc finger domain``.sequence.Value
    let seq2 = human2.``Probable helicase with zinc finger domain``.sequence.Value
    Assert.AreEqual(genes1, genes2)
    Assert.AreEqual(seq1, seq2)

[<Test>]
let ``Non existing keyword returns empty result`` () =
    let q = UniProtProvider.ByKeyWord<"qqqqqq">() // does not fail with error
    Assert.AreEqual(true, true)

[<Test>]
let ``Direct access and suggested query return the same`` () =
    let keratin1 = UniProtProvider.ByKeyWord<"keratin">().``Keratin, type I cytoskeletal 14``.primaryAccession
    let keratin2 = UniProtProvider.ByKeyWord<"kerain">().keratin.``Keratin, type I cytoskeletal 14``.primaryAccession
    Assert.AreEqual(keratin1, keratin2)

[<Test>]
let ``Result related to specific organism is found after filtering by that organism`` () =
    let keratin1 = UniProtProvider.ByKeyWord<"keratin">().``Keratin-like protein KRT222``.primaryAccession
    let keratin2 = UniProtProvider.ByKeyWord<"keratin">().ByOrganism<"human">().``Keratin-like protein KRT222``.primaryAccession
    Assert.AreEqual(keratin1, keratin2)

[<Test>]
let ``Multiple filters by organism are rewritten`` () =
    let keratin1 = UniProtProvider.ByKeyWord<"keratin">().ByOrganism<"mouse">().``Keratin, type II cytoskeletal 2 epidermal``.primaryAccession
    let keratin2 = UniProtProvider.ByKeyWord<"keratin">().ByOrganism<"mou">().ByOrganism<"mouse">().``Keratin, type II cytoskeletal 2 epidermal``.primaryAccession
    Assert.AreEqual(keratin1, keratin2)
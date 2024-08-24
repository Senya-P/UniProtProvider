module UniProtProviderTests
open UniProtProvider
open NUnit.Framework

type Assert() =
    static member AreEqual(a, b) = if a <> b then failwith "oops"

[<Test>]
let ``ById and ByKeyWord return the same`` () =

    let human1 = UniProtProvider.ById<"P42694">()
    let human2 = UniProtProvider.ByKeyWord<"Human">()
    let genes1 = human1.``Probable helicase with zinc finger domain (HELZ_HUMAN)``.genes.Value
    let genes2 = human2.``Probable helicase with zinc finger domain (HELZ_HUMAN)``.genes.Value
    let seq1 = human1.``Probable helicase with zinc finger domain (HELZ_HUMAN)``.sequence.Value
    let seq2 = human2.``Probable helicase with zinc finger domain (HELZ_HUMAN)``.sequence.Value
    Assert.AreEqual(genes1, genes2)
    Assert.AreEqual(seq1, seq2)

[<Test>]
let ``Non existing keyword returns empty result`` () =
    let q = UniProtProvider.ByKeyWord<"qqqqqq">() // does not fail with error
    Assert.AreEqual(true, true)

[<Test>]
let ``Direct access and suggested query return the same`` () =
    let keratin1 = UniProtProvider.ByKeyWord<"keratin">().``Keratin, type I cytoskeletal 14 (K1C14_HUMAN)``.primaryAccession
    let keratin2 = UniProtProvider.ByKeyWord<"kerain">().keratin.``Keratin, type I cytoskeletal 14 (K1C14_HUMAN)``.primaryAccession
    Assert.AreEqual(keratin1, keratin2)

[<Test>]
let ``Result related to specific organism is found after filtering by that organism`` () =
    let keratin1 = UniProtProvider.ByKeyWord<"keratin">().``Keratin-like protein KRT222 (KT222_HUMAN)``.primaryAccession
    let keratin2 = UniProtProvider.ByKeyWord<"keratin">().ByOrganism<"human">().``Keratin-like protein KRT222 (KT222_HUMAN)``.primaryAccession
    Assert.AreEqual(keratin1, keratin2)

[<Test>]
let ``Multiple filters by organism are rewritten`` () =
    let keratin1 = UniProtProvider.ByKeyWord<"keratin">().ByOrganism<"mouse">().``Keratin, type I cytoskeletal 18 (K1C18_MOUSE)``.primaryAccession
    let keratin2 = UniProtProvider.ByKeyWord<"keratin">().ByOrganism<"mou">().ByOrganism<"mouse">().``Keratin, type I cytoskeletal 18 (K1C18_MOUSE)``.primaryAccession
    Assert.AreEqual(keratin1, keratin2)

[<Test>]
let ``Show more is available`` () =
    let human = UniProtProvider.ByKeyWord<"Human">().``More...``.``More...``.``More...`` // does not fail with error
    Assert.AreEqual(true, true)

[<Test>]
let ``Results can be found with show more`` () =
    let keratin1 = UniProtProvider.ByKeyWord<"inulin">().ByOrganism<"human">().``Regenerating islet-derived protein 3-gamma (REG3G_HUMAN)``.primaryAccession
    let keratin2 = UniProtProvider.ByKeyWord<"inulin">().``More...``.``Regenerating islet-derived protein 3-gamma (REG3G_HUMAN)``.primaryAccession
    Assert.AreEqual(keratin1, keratin2)


let insulin = UniProtProvider.ByKeyWord<"insulin">().``Insulin (INS_HUMAN)``.proteinDescription.contains.Value
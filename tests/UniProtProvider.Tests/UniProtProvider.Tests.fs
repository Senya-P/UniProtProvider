module UniProtProviderTests
open UniProtProvider
open NUnit.Framework

type Assert() =
    static member AreEqual(a, b) = if a <> b then failwith "Objects are not equal"

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

[<Test>]
let ``Search by organism and by taxon id return the same`` () =
    let human1 = UniProtProvider.ByOrganism<"human">().``Homo sapiens (9606)``.``Homo sapiens (9606)``.scientificName
    let human2 = UniProtProvider.ByTaxonId<9606>().``Homo sapiens (9606)``.scientificName
    Assert.AreEqual(human1, human2)

[<Test>]
let ``Proteins related to the organism are listed`` () =
    let protein1 = UniProtProvider.ByOrganism<"human">().``Homo sapiens (9606)``.FindRelated().``Clarin-2 (CLRN2_HUMAN)``.primaryAccession
    let protein2 = UniProtProvider.ByKeyWord<"Clarin-2">().ByOrganism<"human">().``Clarin-2 (CLRN2_HUMAN)``.primaryAccession
    Assert.AreEqual(protein1, protein2)
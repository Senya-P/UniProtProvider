#r "src/UniProtProvider.DesignTime/bin/Debug/netstandard2.1/UniProtProvider.DesignTime.dll"

open UniProtProvider

let [<Literal>] seq  = "UPI000000000A"

type Uniparc = UniParcProvider<seq>

(*
let obj = Uniparc
                .uniParcId
                    .uiniParcCrossReferences
                    .sequence.value
                            .length
                            .molWeight
                            .crc64
                            .md5
                    .sequenceFeatures
                    .oldestCrossRefCreated
                    .ostRecentCrossRefUpdated
*)
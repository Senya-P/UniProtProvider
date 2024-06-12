module UniProtprovider.ExampleProgram
open UniProtProvider
type Assert() =
    static member AreEqual(a, b) = if a <> b then failwith "oops"

type UniProtKB = UniProtKBProvider
let obj = UniProtKB.ById("P68452")
let md5 = obj.sequence.md5

let w = obj.comments

for i in w do 
    printf "%s" i.commentType 
    printf "%A" i.texts
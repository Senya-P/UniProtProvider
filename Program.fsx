#r @"C:/Users/Senya/Documents/code/6sem/TypeProviders/UniProtProvider/src/UniProtProvider.RunTime/bin/Release/netstandard2.1/UniProtProvider.RunTime.dll"  //runtime
#r "nuget: FSharp.Json, 0.4.1"
#r "nuget: System.Data.HashFunction.CityHash, 2.0.0"
open System
open UniProtProvider.RunTime
open UniProtProvider.RunTime.TypeGenerator

//let result = request "https://rest.uniprot.org/uniprotkb/search?query=human+AND+(reviewed:true)&size=5"


let param = TypeGenerator.Params("human")
param.organism <- "human"

let getPath (url: string) =
    let cityHash = System.Data.HashFunction.CityHash.CityHashFactory.Instance.Create(System.Data.HashFunction.CityHash.CityHashConfig(HashSizeInBits = 64))
    let hash = cityHash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(url))
    let hashedValue = hash.AsHexString()
    //let mutable currentDirectory = System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory())
    let mutable currentDirectory = System.IO.DirectoryInfo("C:/Users/Senya/Documents/code/6sem/TypeProviders/UniProtProvider")
    while currentDirectory.Parent <> null && currentDirectory.Name <> "UniProtProvider" do
        currentDirectory <- currentDirectory.Parent
        //printf "%s\n" currentDirectory.FullName
    let path = System.IO.Path.Combine(currentDirectory.FullName, "tmp")
    if not (System.IO.Directory.Exists(path)) then
        System.IO.Directory.CreateDirectory(path) |> ignore
    System.IO.Path.Combine(path, hashedValue)


printf "%s\n" (getPath "https://rest.uniprot.org/uniprotkb/search?query=human+AND+(reviewed:true)&size=5")

// 751a808d74079826
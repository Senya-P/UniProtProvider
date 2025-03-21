namespace Cache

open System.IO
open System.IO.Compression

// --------------------------------------------------------------------------------------
// Helper class for caching mechanism implementation
// --------------------------------------------------------------------------------------
module Cache =
    open System

    let private tmpDir = 
        let path = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "tmp")
        Directory.CreateDirectory path |> ignore
        path

    let private getHashedValue (url:string) =
        let cityHash = 
            System.Data.HashFunction.CityHash.CityHashFactory.Instance.Create(
                System.Data.HashFunction.CityHash.CityHashConfig(HashSizeInBits = 64)
            )
        let hash = cityHash.ComputeHash(
            System.Text.Encoding.ASCII.GetBytes(url)
        )
        hash.AsHexString()

    /// Constructs the file path for caching based on the hashed URL
    let private getPath (url: string) =
        let hashedValue = getHashedValue url
        System.IO.Path.Combine(tmpDir, hashedValue)

    let cacheResult (url: string, contents: Stream) = async {
        let path = getPath url
        use fileStream = new FileStream(
            path, 
            FileMode.Create, 
            FileAccess.Write,
            FileShare.None,
            bufferSize = 4096,
            useAsync = true
        )
        do! contents.CopyToAsync(fileStream) |> Async.AwaitTask
        do! fileStream.FlushAsync() |> Async.AwaitTask
    }

    let getCachedResult (url: string) = async {
        let path = getPath url
        // check timestemps and delete old?
        if File.Exists path then
            use compressedFileStream = File.Open(path, FileMode.Open, FileAccess.Read)
            use decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress)
            use reader =  new StreamReader(decompressor)
            return! reader.ReadToEndAsync() |> Async.AwaitTask
        else 
            return ""
    }
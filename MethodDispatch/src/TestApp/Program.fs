open System
open MethodDispatcher

[<ExposeMethod>]
let testMethod (str: string) = printfn "%s" str

[<EntryPoint>]
let main argv =
    let getJob () = Console.ReadLine()
    let postReply reply = printfn "%s" reply

    printfn "%s" (MethodDispatcher.GetSerializedExternalDeclaration())

    MethodDispatcher(getJob, postReply).Start()

    0
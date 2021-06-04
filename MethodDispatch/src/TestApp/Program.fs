open System
open MethodDispatcher

[<EntryPoint>]
let main argv =
    let getJob () = Console.ReadLine()
    let postReply reply = printfn "%s" reply

    MethodDispatcher(getJob, postReply).Start()

    0
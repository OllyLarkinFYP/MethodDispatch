open System
open MethodDispatcher

[<ExposeMethod>]
let testMethodFast (str: string) =
    printfn "%s" str

[<ExposeMethod>]
let testMethodSlow (str: string) =
    Threading.Thread.Sleep 1000
    printfn "%s" str

[<ExposeMethod>]
let testMethodSlowest (str: string) =
    Threading.Thread.Sleep 5000
    printfn "%s" str

[<ExposeMethod>]
let testIntParam (i: int) =
    printfn "integer: %i" i

// {"id":0,"methodName":"testMethodSlowest","parameters":["slowest"]}
// {"id":1,"methodName":"testMethodSlow","parameters":["slow"]}
// {"id":2,"methodName":"testMethodFast","parameters":["fast"]}

[<EntryPoint>]
let main argv =
    let getJob () = Console.ReadLine()
    let postReply reply = printfn "%s" reply

    printfn "%s" (MethodDispatcher.GetSerializedExternalDeclaration())

    MethodDispatcher(getJob, postReply).Start()

    0
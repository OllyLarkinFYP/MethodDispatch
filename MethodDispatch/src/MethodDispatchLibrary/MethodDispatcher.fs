namespace MethodDispatcher

open System

type MethodDispatcher (getJob: unit -> string, postReply: string -> unit) =
    let replyQueue = 
        MailboxProcessor.Start (fun inbox ->
            let rec loop () =
                async { 
                    let! reply = inbox.Receive()
                    postReply reply
                    return! loop () 
                }
            loop ())

    let processMethodRequest methodReq = raise <| NotImplementedException()

    member _.Start () =
        while true do
            let job = getJob()
            let processJob =    
                async {
                    let reply = processMethodRequest job
                    replyQueue.Post(reply)
                }
            Async.Start processJob

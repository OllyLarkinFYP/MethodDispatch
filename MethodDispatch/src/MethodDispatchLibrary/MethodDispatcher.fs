namespace MethodDispatcher

open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type IncomingMethodCall = 
    { [<JsonProperty(Required = Required.Always)>]
      id: int

      [<JsonProperty(Required = Required.Always)>]
      methodName: string

      [<JsonProperty(Required = Required.Always)>]
      parameters: JArray }

type OutgoingMethodReply =
    { id: int
      reply: obj }


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

    static let internalDeclaration = Declarations.getDeclaration()
    static let externalDeclaration = Declarations.getExternalDeclaration internalDeclaration

    let executeMethod methodName (parameters: obj []) : obj =
        internalDeclaration.[methodName].method.Invoke(null, parameters)

    let parseParams methodName (parameters: JArray) : Result<obj [], string> =
        let paramArray = Seq.toArray parameters
        let method = internalDeclaration.[methodName]
        if method.parameters.Length <> paramArray.Length
        then 
            Error <| 
                sprintf "The wrong number of parameters were provided for the method '%s'. %s expects %i parameters, but %i were provided." 
                    methodName 
                    methodName 
                    method.parameters.Length 
                    paramArray.Length
        else
            (method.parameters, paramArray)
            ||> Array.zip
            |> Array.map (fun (paramInfo, jToken) ->
                jToken.ToObject paramInfo.paramType)
            |> Ok

    let processMethodRequest methodCall =
        let parsedCall = JsonConvert.DeserializeObject<IncomingMethodCall> methodCall   // TODO: handle exceptions
        if internalDeclaration.ContainsKey parsedCall.methodName
        then
            parsedCall.parameters
            |> parseParams parsedCall.methodName
            |> function
            | Error e -> Error e
            | Ok parameters ->
                { id = parsedCall.id
                  reply = executeMethod parsedCall.methodName parameters }
                |> JsonConvert.SerializeObject
                |> Ok
        else Error <| sprintf "The method '%s' cannot be found. Is it exposed with the [<ExposeMethod>] attribute?" parsedCall.methodName
                
    member _.Start () =
        while true do
            let job = getJob()
            let processJob =
                async {
                    processMethodRequest job
                    |> function
                    | Ok reply -> replyQueue.Post(reply)
                    | Error e -> failwith e     // TODO: handle differently?
                }
            Async.Start processJob

    static member ExternalDeclaration = externalDeclaration
    static member GetSerializedExternalDeclaration () = Declarations.getSerializedExternalDeclaration externalDeclaration

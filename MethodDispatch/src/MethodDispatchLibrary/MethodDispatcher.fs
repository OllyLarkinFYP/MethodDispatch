namespace MethodDispatcher

open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Utils

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

    let printError (str: string) =
        let handle = "[MethodDispatcher] Error: "
        str.Split([|'\n'|])
        |> Array.map (fun s -> handle + s + "\n")
        |> Array.fold (+) ""
        |> eprintf "%s"

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
            |> Array.resMap (fun (paramInfo, jToken) ->
                fun () -> jToken.ToObject paramInfo.paramType
                |> tryToResult
                |> function
                | Ok p -> Ok p
                | Error e -> Error <| sprintf "Could not parse parameter to correct type.")

    let processMethodRequest methodCall =
        fun () -> JsonConvert.DeserializeObject<IncomingMethodCall> methodCall
        |> Utils.tryToResult
        |> function
        | Error e -> Error <| sprintf "Unable to deserialize method call: \n%s" e.Message
        | Ok parsedCall ->
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
                    | Error e -> printError e
                }
            Async.Start processJob

    static member ExternalDeclaration = externalDeclaration
    static member GetSerializedExternalDeclaration () = Declarations.getSerializedExternalDeclaration externalDeclaration

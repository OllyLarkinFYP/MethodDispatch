namespace MethodDispatcher

open System
open System.Reflection
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Utils

module Declarations =

    type GenericMethodException (methodName: string) =
        inherit Exception(sprintf "The method '%s' uses generics, which are not supported by this tool." methodName)

    type DuplicateMethodNameException (methodName: string) =
        inherit Exception(sprintf "The method name '%s' has been used multiple times. This is not allowed. Please use the format '[<ExposeMethod(\"example_name\")>]' to export a different name." methodName)

    type InternalParam = {
        name: string
        paramType: System.Type
    }

    type ExternalParam = {
        name: string
        paramType: string
    }

    type InternalDeclarationElement = {
        returnType: System.Type
        parameters: InternalParam array
        method: MethodInfo
    }
    type InternalDeclaration = Map<string, InternalDeclarationElement>

    type ExternalDeclarationElement = {
        name: string
        returnType: string
        parameters: ExternalParam array
    }
    type ExternalDeclaration = ExternalDeclarationElement array

    let private generateDeclaration (methods: (string * MethodInfo) array) : InternalDeclaration =
        methods
        |> Array.map (fun (name, method) ->
            let parameters =
                method.GetParameters()
                |> Array.map (fun param -> { InternalParam.name = param.Name; paramType = param.ParameterType })
            name,
                { returnType = method.ReturnType
                  parameters = parameters
                  method = method })
        |> Map.ofArray

    let getDeclaration () =
        let validate methods =
            methods
            |> Array.iter (fun (name: string, method: MethodInfo) ->
                if method.IsGenericMethod || method.ContainsGenericParameters
                then raise <| GenericMethodException name)
            methods
            |> Array.countBy fst
            |> Array.iter (fun (name, count) ->
                if count > 1
                then raise <| DuplicateMethodNameException name)

        let methods =
            AppDomain.CurrentDomain.GetAssemblies()
            |> Array.collect (fun assembly -> assembly.GetTypes())
            |> Array.collect (fun typ -> typ.GetMethods())
            |> Array.choose (fun method ->
                method.GetCustomAttribute<ExposeMethodAttribute>()
                |> nullableToOption
                |> Option.map (fun attr -> attr.Name, method))
        validate methods
        generateDeclaration methods

    let private exportDeclaration : InternalDeclaration -> ExternalDeclaration =
        let convertParams =
            Array.map (fun (p: InternalParam) ->
                { ExternalParam.name = p.name
                  paramType = p.paramType.ToString() })
        Map.toArray
        >> Array.map (fun (name, dec) ->
            { name = name
              returnType = dec.returnType.ToString()
              parameters = convertParams dec.parameters })

    let private serialize (dec: ExternalDeclaration) =
        JsonConvert.SerializeObject(dec)

    let getExternalDeclaration inDec =
        inDec
        |> exportDeclaration

    let getSerializedExternalDeclaration exDec =
        exDec
        |> serialize
            
    let exportExternalDeclaration exDec (path: string) =
        let json = getSerializedExternalDeclaration exDec
        File.WriteAllText(Path.Combine(path, "export.declaration.json"), json)

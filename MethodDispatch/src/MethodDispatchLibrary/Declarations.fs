namespace MethodDispatcher

open System
open System.Reflection
open System.IO
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Utils

module Declarations =
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
                then failwithf "Generics are currently not supported for exposed methods: %s,\n%A" name method)
            let repNames =
                Array.countBy fst methods
                |> Array.choose (fun (name, num) ->
                    if num > 1
                    then Some name
                    else None)
            if repNames.Length <> 0
            then failwithf "The following names were declared multuple times: %A\nUse the format [<ExposeMethod(\"example_name\")>] to expose with a different name." repNames

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

namespace MethodDispatcher

open System

module Utils =
    let nullableToOption a =
        match box a with
        | null -> None
        | _ -> Some a

    let tuple a b = a, b

    let tryToResult f =
        try
            Ok <| f ()
        with
        | e -> Error e

    module Array =
        let resMap (mapFunc: 'T -> Result<'Res,'Err>) (arr: 'T array) : Result<'Res array, 'Err> =
            let rec resMapRec (lst: 'T list) : Result<'Res list, 'Err> =
                match lst with
                | [] -> Ok []
                | hd::tl ->
                    match mapFunc hd with
                    | Ok procHd ->
                        match resMapRec tl with
                        | Ok procTl -> Ok (procHd::procTl)
                        | Error err -> Error err
                    | Error err -> Error err
            arr
            |> List.ofArray
            |> resMapRec
            |> Result.bind (Array.ofList >> Ok)

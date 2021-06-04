namespace MethodDispatcher

open System

module Utils =
    let nullableToOption a =
        match box a with
        | null -> None
        | _ -> Some a

    let tuple a b = a, b

namespace MethodDispatcher

open System

/// Currently not in use but future plans to incorporate
[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type ExposeTypeAttribute() = 
    inherit Attribute()

namespace Shared

type Counter = { Value : int }
// attribute Required for giraffe's built-in serializer
[<CLIMutable>]
type LoginModel = {username:string;password:string}


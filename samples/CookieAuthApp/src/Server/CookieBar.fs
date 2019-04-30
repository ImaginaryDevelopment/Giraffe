module CookieAuthApp.CookieBar
open Microsoft.Extensions.Primitives // for StringValues
open Microsoft.AspNetCore.Authentication

// added to the project via
// in app root direction run:
// dotnet tool install paket -g
// paket add -g Server Microsoft.AspNetCore.Authentication.Cookies
// had to manually go into ./Server/paket.references and add it there
open Microsoft.AspNetCore.Authentication.Cookies

open FSharp.Control.Tasks.V2
open Giraffe
open Giraffe.HttpStatusCodeHandlers
open System.Security.Claims

// this is known as a full active pattern for more see: https://fsharpforfunandprofit.com/posts/convenience-active-patterns/
// why is a `StringValues` type returned for form values? https://stackoverflow.com/questions/48188934/why-is-stringvalues-used-for-request-query-values
let (|NoValue|ASingleValue|MultipleValues|) (svOpt:Microsoft.Extensions.Primitives.StringValues option)=
    svOpt
    |> Option.map (fun sv ->
        match sv.Count with
        | 0 -> NoValue
        | 1 -> ASingleValue sv.[0]
        | _ -> MultipleValues (sv |> Seq.cast<string>)
    )
    |> function
        |Some x -> x
        |None -> NoValue

let pretendAuthentication un pwd = un = "main" && pwd = "1234"

// actual handler
let signin :HttpHandler =

    (fun next ctx ->
        printfn "We are running signin!"
        task{
            let! form = ctx.Request.ReadFormAsync()
            printfn "We read the form?"

            let tryGetKey k = match form.TryGetValue k with | true, v -> Some v | _ -> None

            match tryGetKey "username", tryGetKey "password" with
            | ASingleValue un, ASingleValue pwd ->
                // I have no idea what realm is supposed to mean
                let realm = if ctx.Request.Host.HasValue then ctx.Request.Host.Value else "localhost"
                let authScheme = CookieAuthenticationDefaults.AuthenticationScheme
                printfn "pretendAuth time"
                // stub oversimplified username/password check instead of checking a real database
                match pretendAuthentication un pwd with
                | false ->
                    // not security minded, but may be helpful for diagnostics while learning
                    return! RequestErrors.UNAUTHORIZED authScheme realm (sprintf "User %s is not authorized" un) next ctx
                | true ->
                    // just a sample that works, not a recommendation
                    let issuer = sprintf "%s://%s" ctx.Request.Scheme ctx.Request.Host.Value
                    let claims = [
                        Claim(ClaimTypes.Name, un, ClaimValueTypes.String, issuer)
                    ]
                    let identity = ClaimsIdentity(claims,authScheme)
                    let user = ClaimsPrincipal identity
                    // tell aspnet that the user is authenticated
                    do! ctx.SignInAsync(authScheme, user)
                    return! text (sprintf "Successfully logged in %s" un) next ctx
            | NoValue, NoValue -> return! RequestErrors.BAD_REQUEST "Neither username nor password were included in post" next ctx
            | NoValue, _ -> return! RequestErrors.BAD_REQUEST "Password was not included in post" next ctx
            | _, NoValue -> return! RequestErrors.BAD_REQUEST "Username was not included in post" next ctx
            | MultipleValues _, _ -> return! RequestErrors.BAD_REQUEST "Username read error" next ctx
            | _, MultipleValues _ -> return! RequestErrors.BAD_REQUEST "Password read error" next ctx


        }
    )
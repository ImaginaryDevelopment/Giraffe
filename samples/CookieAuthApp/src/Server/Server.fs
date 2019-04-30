// expected dotnet sdk is already install, and the SAFE template
// template created by `dotnet new SAFE --server giraffe --layout none`
// tools added:
// `dotnet tool install -g Fake`
// `dotnet tool install -g Paket`
// packages added:
// `paket add -g Server Microsoft.AspNetCore.Authentication.Cookies`

open System
open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open FSharp.Control.Tasks.V2
open Giraffe
open Shared
open Microsoft.AspNetCore.Authentication.Cookies


let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"
let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let getInitCounter () : Task<Counter> = task { return { Value = 42 } }
// a part we can use to abort the current match route and attempt whatever is next in the choose/pipeline
let never:HttpHandler= fun _ _ -> Task.FromResult None
// a part we can sprinkle anywhere to do stuff when routing tries to go through this path
let diagPart msg handler :HttpHandler =
    (fun next ctx ->
        printfn "Routing : %s in %s %A" msg ctx.Request.Method ctx.Request.Path
        handler next ctx
    )
let webApp =
    choose [

        GET >=> choose[
            route "/api/init" >=>
                fun next ctx ->
                    task {
                        let! counter = getInitCounter()
                        return! Successful.OK counter next ctx
                    }

        ]
        POST >=> choose [
            diagPart "checking post options" never
            route "/api/authentication/login" >=> CookieAuthApp.CookieBar.signin
            diagPart "rolled right past signin" never
        ]
    ]

let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore
    services.AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(Thoth.Json.Giraffe.ThothSerializer()) |> ignore
    // without this, on authenitcation attempt, we get an exception that `no service for type 'Microsoft.AspNetCore.Authentication.IAuthenticationServices' has been registered.`
    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie() |> ignore

WebHost
    .CreateDefaultBuilder()
    .UseWebRoot(publicPath)
    .UseContentRoot(publicPath)
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
    .Build()
    // to startup from the project root directory (omit tick marks)
    // `dotnet tool install -g Fake`
    // `fake build --target run`
    .Run()
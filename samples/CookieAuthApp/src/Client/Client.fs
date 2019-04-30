module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Thoth.Json

open Shared
open Fable.PowerPack
open System.Net.Http



module RProps = Fable.Helpers.React.Props

// The model holds data that you want to keep track of while the application is running
type AjaxModel = {Username:string;Password:string;AjaxStatusMessage:string}
type Model = { Counter: Counter option;AjaxModel:AjaxModel}

type AjaxMessage =
    | UpdateUsername of string
    | UpdatePassword of string
    | SendAjax
    | Response of Result<string,string>
// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Increment
| Decrement
| InitialCountLoaded of Result<Counter, exn>
| AjaxMessage of AjaxMessage



let initialCounter = fetchAs<Counter> "/api/init" (Decode.Auto.generateDecoder())

// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { Counter = None
                         // username and password must never be null, React requires bound components to always have a value
                         AjaxModel={Username="";Password="";AjaxStatusMessage=null}}
    let loadCountCmd =
        Cmd.ofPromise
            initialCounter
            []
            (Ok >> InitialCountLoaded)
            (Error >> InitialCountLoaded)
    initialModel, loadCountCmd


let updateAjax msg (m:AjaxModel):AjaxModel*Cmd<AjaxMessage> =
    printfn "update ajax time"
    match msg with
    | AjaxMessage.UpdateUsername un ->
        {m with Username = un},Cmd.none
    | AjaxMessage.UpdatePassword pwd ->
        {m with Password = pwd},Cmd.none
    | AjaxMessage.Response(Ok msg) ->
        {m with AjaxStatusMessage = msg}, Cmd.none
    | AjaxMessage.Response(Error msg) ->
        {m with AjaxStatusMessage = msg}, Cmd.none

    | AjaxMessage.SendAjax ->
        printfn "Ajaxing!"
        let url = "/api/authentication/login"
        let requestProps = [
                RequestProperties.Method HttpMethod.POST
                requestHeaders [ContentType "application/json"]
                // if we were using model binding this would work fine
                RequestProperties.Body (unbox(Thoth.Json.Encode.Auto.toString(space=0, value={username=m.Username;password=m.Password},forceCamelCase=true)))
            ]
        printfn "Request Props made"

        let loginPromise ()= //fetch "/api/authentication/login"
            promise {
                let! response = fetch url requestProps
                printfn "Response found"
                let! text = response.text()
                printfn "text read"
                return text
            }
        let cmd = Cmd.ofPromise loginPromise () (fun msg -> printfn "Yay response ok!"; msg |> Ok |> AjaxMessage.Response) (fun ex -> printfn "Failed exception"; ex.Message |> Error |> AjaxMessage.Response)
        m, cmd
// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel.Counter, msg with
    | Some counter, Increment ->
        let nextModel = { currentModel with Counter = Some { Value = counter.Value + 1 } }
        nextModel, Cmd.none
    | Some counter, Decrement ->
        let nextModel = { currentModel with Counter = Some { Value = counter.Value - 1 } }
        nextModel, Cmd.none
    | _, InitialCountLoaded (Ok initialCount)->
        let nextModel = { currentModel with Counter = Some initialCount }
        nextModel, Cmd.none
    | _, AjaxMessage ajaxMsg ->
        printfn "Update -> updateAjax"
        let m,ajaxCmd = updateAjax ajaxMsg currentModel.AjaxModel
        let cmd = Cmd.map AjaxMessage ajaxCmd
        {currentModel with AjaxModel = m}, cmd
    | _ -> currentModel, Cmd.none


let safeComponents =
    let components =
        span [ ]
           [
             a [ Href "https://github.com/giraffe-fsharp/Giraffe" ] [ str "Giraffe" ]
             str ", "
             a [ Href "http://fable.io" ] [ str "Fable" ]
             str ", "
             a [ Href "https://elmish.github.io/elmish/" ] [ str "Elmish" ]
           ]

    span [ ]
        [ strong [] [ str "SAFE Template" ]
          str " powered by: "
          components ]

let show = function
| { Counter = Some counter } -> string counter.Value
| { Counter = None   } -> "Loading..."

let loginComponent (model:Model) (dispatch:Msg -> unit) =
    let getValue (e:Fable.Import.React.FormEvent) = e.Value

    let makeSection title content =
        section [][
            h2 [] [ str title ]
            div [] content
        ]
    div [] [
        hr []
        makeSection "Non-Ajax version" [
            form[Id "noajax";RProps.Method "post";RProps.Action "/api/authentication/login"][
                label [] [str "Username:"]
                input [Name "username"]
                label [] [str "Password:"]
                input [RProps.Type "password"; Name"password"]
                input [RProps.Type "submit";Value "Login"]
            ]
        ]
        hr[]
        makeSection "Ajax version" [
            // if the below is wrapped in a form, the form has values and it gets a 401 unauthorized from the ajax call
            // then it navigates to the current page with the values added as a query string
            //  which restarts the elm app. (stored values/model is lost and reinitialized)
            form[Id "ajax"] (
                let ajaxDispatch = AjaxMessage >> dispatch
                let onAjaxChange messageWrapper =
                    getValue
                    >> messageWrapper
                    >> ajaxDispatch
                [
                    label [] [str "Username:"]
                    input [Name "username"; DefaultValue model.AjaxModel.Username; OnChange (onAjaxChange AjaxMessage.UpdateUsername)]
                    label [] [str "Password:"]
                    input [RProps.Type "password"; Name"password";DefaultValue model.AjaxModel.Password; OnChange(onAjaxChange AjaxMessage.UpdatePassword)]
                    button [
                        match model.AjaxModel.Username with
                        | null | "" -> ()
                        | _ -> yield RProps.OnClick (fun _ -> AjaxMessage.SendAjax |> AjaxMessage |> dispatch)][ str "Login"]
                    div [] [str model.AjaxModel.AjaxStatusMessage]
                ]
            )
        ]
        hr[]
    ]

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ h1 [] [ str "SAFE Template" ]
          p  [] [ str "The initial counter is fetched from server" ]
          p  [] [ str "Press buttons to manipulate counter:" ]
          button [ OnClick (fun _ -> dispatch Decrement) ] [ str "-" ]
          div [] [ str (show model) ]
          button [ OnClick (fun _ -> dispatch Increment) ] [ str "+" ]
          loginComponent model dispatch
          safeComponents ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run

module Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Thoth.Json

open Shared



module RProps = Fable.Helpers.React.Props
// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { Counter: Counter option }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
| Increment
| Decrement
| InitialCountLoaded of Result<Counter, exn>



let initialCounter = fetchAs<Counter> "/api/init" (Decode.Auto.generateDecoder())

// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { Counter = None }
    let loadCountCmd =
        Cmd.ofPromise
            initialCounter
            []
            (Ok >> InitialCountLoaded)
            (Error >> InitialCountLoaded)
    initialModel, loadCountCmd



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
        let nextModel = { Counter = Some initialCount }
        nextModel, Cmd.none

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

let loginComponent model dispatch =
    div [] [
        hr []
        section [][
            h2 [] [
                str "Non-Ajax version"

            ]
            div [] [

                form[RProps.Method "post";RProps.Action "/api/authentication/login"][
                    label [] [str "Username:"]
                    input [Name "username"]
                    label [] [str "Password:"]
                    input [RProps.Type "password"; Name"password"]
                    input [RProps.Type "submit";Value "Login"]
                ]

            ]
        ]

        hr[]
        // section [][
        //     h2 [] [ str "Ajax version"]
        //     div [][

        //     ]
        // ]
        // hr[]

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

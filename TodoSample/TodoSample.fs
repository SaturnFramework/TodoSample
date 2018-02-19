module TodoSample

open Saturn
open Giraffe
open FSharp.Control.Tasks.ContextInsensitive
open System

type Todo = 
  { mutable title : string
    mutable order : int
    mutable completed : bool
    mutable url : string }

let store = ResizeArray<Todo>()
let find (id : string) = store |> Seq.tryFind (fun t -> t.url.EndsWith id)

let todoController = controller {
    index (fun ctx -> 
        Controller.json ctx store)
    
    show (fun (ctx, id) -> find id |> Controller.json ctx)
    
    create (fun ctx -> task {
        let! input = Controller.getModel<Todo> ctx
        let input = {input with url = sprintf "%s%O" ctx.Request.Path.Value (Guid.NewGuid()) }
        store.Add input
        return! Controller.json ctx input })
    
    update (fun (ctx, id) -> task {
        let! input = Controller.getModel<Todo> ctx
        match find id with
        | Some old ->
            old.title <- input.title
            old.order <-input.order
            old.completed <- input.completed    
        | None -> ()
        return! Controller.json ctx input    
    })

    delete (fun (ctx,id) -> 
        match find id with
        | Some res -> store.Remove res |> ignore
        | None -> ()
        Controller.json ctx "")
    
    delete_all (fun ctx ->
        store.Clear()
        Controller.json ctx store)
}

let topRouter = scope {
    forward "/todos" todoController
}

let app = application {
    router topRouter
    use_cors "CORS" (fun builder -> builder.WithOrigins("*").AllowAnyMethod().WithHeaders("content-type") |> ignore)
    url "http://0.0.0.0:8085/" 
    service_config (fun s -> s.AddGiraffe())
}

[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code

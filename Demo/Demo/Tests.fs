module Tests

open Swensen.Unquote
open Xunit

open Demo.Domain

[<Fact>]
let ``tryCreatePerson with all values present`` () =
    let personResult = tryCreatePerson "Lisa" "Simpson" "lisa rocks"
    match personResult with
    | Ok person ->
        person.FirstName =! Some (FirstName "Lisa")
        person.LastName =! Some (LastName "Simpson")
        person.UserName |> UserName.value =! "lisa rocks"
    | Error _ ->
        true =! false

[<Fact>]
let ``tryCreatePerson with missing optional values`` () =
    let personResult = tryCreatePerson "" "" "lisa rocks"
    match personResult with
    | Ok person ->
        person.FirstName =! None
        person.LastName =! None
        person.UserName |> UserName.value =! "lisa rocks"
    | Error _ ->
        true =! false
        
[<Theory>]
[<InlineData("user name is too long")>]
[<InlineData("")>]
let ``tryCreatePerson with invalid user names`` invalidUserName =
    let personResult = tryCreatePerson "" "" invalidUserName
    match personResult with
    | Ok _ ->
        true =! false
    | Error e ->
        e =! $"Problem creating Person. UserName is invalid: '{invalidUserName}'."

module Demo.Domain
open System

type FirstName = FirstName of string
type LastName = LastName of string

type UserName = private UserName of string
module UserName =
    let isValid s = not (String.IsNullOrEmpty(s)) && s.Length < 50
    let create (str: string) =
        if isValid str then
            Some (UserName str)
        else
            None
    let value (UserName str) = str

type Person = {
    FirstName: FirstName option
    LastName: LastName option
    UserName: UserName
}

let fn = ""
let ln = ""
let un = ""

let tryCreatePerson fn ln un =
    let maybeUserName un = 
        match UserName.create un with
        | Some validNameUserName -> Some validNameUserName
        | None -> None

    let maybeFirstName fn =
        match fn with
        | Some validNameFirstName -> Some validNameFirstName
        | None -> None
        
    let maybeLastName ln =
        match ln with
        | Some validNameLastName -> Some validNameLastName
        | None -> None
        
    match maybeUserName un with
    | Some validNameUserName ->
        {  }

let maybeUser1 = tryCreatePerson "Lisa" "Simpson" "lisa rocks"
let maybeUser2 = tryCreatePerson "Homer" "Simpson" ""
let maybeUser3 = tryCreatePerson "Marge" "Simpson" "lisa rocks"

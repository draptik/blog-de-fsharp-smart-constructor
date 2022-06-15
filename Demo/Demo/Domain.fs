module Demo.Domain
open System

type FirstName = FirstName of string
type LastName = LastName of string

type UserName = private UserName of string
module UserName =
    let isValid s = not (String.IsNullOrEmpty(s)) && s.Length <= 10
    
    // smart constructor
    let create (str: string) =
        if isValid str then
            Ok (UserName str)
        else
            Error $"UserName is invalid: '{str}'."
    
    // helper function to extract the string
    let value (UserName str) = str

type Person = {
    FirstName: FirstName option
    LastName: LastName option
    UserName: UserName
}

let tryCreatePerson fn ln un =

    let maybeFirstName fn =
        if String.IsNullOrEmpty(fn) then
            None
        else
            Some (FirstName fn)
        
    let maybeLastName ln =
        if String.IsNullOrEmpty(ln) then
            None
        else
            Some (LastName ln)
        
    match UserName.create un with
    | Error e ->
        Error $"Problem creating Person. {e}"
    | Ok validNameUserName ->
        Ok  {
                FirstName = maybeFirstName fn
                LastName = maybeLastName ln
                UserName = validNameUserName
            }

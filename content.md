# F# Smart Constructor

Das "F# Smart Constructor" Pattern ermöglicht die Erzeugung von validierten F# Typen.

## TL;DR

```fsharp
type UserName = private UserName of string

module UserName =
    let isValid s = // ...

    // smart ctor
    let create (str: string) =
        if isValid str then
            Ok (UserName str)
        else
            Error "Invalid user name"

    // helper function extracting value
    let value (UserName s) = s
```

Anwendung:

```fsharp
let maybeValidUserName name = 
    match UserName.create name with
    | Ok validName -> validName |> UserName.value
    | Error e -> e

maybeValidUserName ""     // -> "Invalid user name"
maybeValidUserName "lisa" // -> "lisa"
```

## Einleitung

In diesem Artikel möchte ich zeigen, wie man in F# Typen mit eingebauter Validierung erzeugt.

Beginnen wir mit einem einfachen `Person` Typ:

```fsharp
type Person = {
    FirstName: string
    LastName: string
}
```

Dies ist ein `record` mit einem `FirstName`, und einem `LastName` (Anmerkung: Seit C# 9 gibt es das Konzept von `record`s auch in C#. Und seit C# 10 gibt es auch `record struct`, was dem F# `record` Konstrukt recht nahe kommt). 

Um eine Instanz dieses Typs zu erzeugen, muss jedem Feld ein Wert zugewiesen werden:

```fsharp
let homer = {
    FirstName = "Homer"
    LastName = "Simpson"
}
```

Möchte man nun eine Formatierungsfunktion anbieten, die mit dem Vor- und Nachnamen arbeitet, könnte eine einfache Implementierung folgendermaßen aussehen:

```fsharp
// string -> string -> string
let formatName firstName lastName = $"{lastName}, {firstName}"
```

Hinweis: Der Kommentar `// string -> string -> string` beschreibt die Typsignatur der darunterstehenden Funktion. Da F# eine bessere Typinferenz als C# hat, muss man meist die Typen nicht explizit angeben. Für diesen Artikel habe ich an manchen Stellen die Signatur als Kommentar hinzugefügt um Unklarheiten zu vermeiden. Der letzte Wert in der Signatur ist der Rückgabewert der Funktion. Alle anderen Werte sind Parameter der Funktion. In C# wäre die entsprechende Signatur `Func<string, string, string>`.

Da es sich beim Vor- und Nachnamen um einen `string` handelt, besteht eine gewisse Verwechslungsgefahr:

```fsharp
// Achtung: Vor- und Nachname sind vertauscht:
let formattedName = formatName "Simpson" "Homer"
```

Dieses Problem wird auch als ["Primitive Obsession"](https://wiki.c2.com/?PrimitiveObsession) Antipattern bezeichnet: Anstatt das vorhandene Typ-System zu nutzen, wird ein `string` verwendet. Abhilfe schafft die Verwendung dedizierter Typen für Vor- und Nachname:

```fsharp
type FirstName = FirstName of string
type LastName = LastName of string
```

Im Gegensatz zum `Person`-Typ handelt es sich hier um sogenannte [Single-Case Discriminated Unions](https://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/).

Der `Person`-Typ kann nun diese dedizierten Typen verwenden:

```fsharp
type Person = {
    FirstName: FirstName
    LastName: LastName
}
```

Um nun eine Instanz dieses Typs zu erzeugen, muss der jeweilige `string` erst in den entsprechenden `FirstName`- und `LastName`-Typen umgewandelt werden:

```fsharp
let bart = {
    FirstName = FirstName "Bart"
    LastName = LastName "Simpson"
}
```

Der `formatName`-Aufruf kann nun ohne Verwechslungsgefahr der Parameterreihenfolge erfolgen, da eine falsche Reihenfolge vom Compiler unterbunden wird:

```fsharp
let formattedName1 = formatName bart.LastName bart.FirstName // -> "Simpson, Bart"
let formattedName2 = formatName (FirstName "Bart") (LastName "Simpson") // -> "Simpson, Bart"
let formattedName3 = formatName (LastName "Simpson") (FirstName "Bart") // 😡 kompiliert nicht
```

## Neue Anforderungen

Szenenwechsel: Aus unerklärlichen Gründen ändern sich die Anforderungen für den `Person`-Typ. Die Angabe eines Vor- und Nachnamens ist nun optional, dafür muss ein `UserName` vorhanden sein. Der `UserName` hat zusätzlich die Anforderung, dass er (a) nicht leer sein darf und (b) nicht mehr als 10 Zeichen lang sein darf.

Die Umsetzung von optionalen Eigenschaften in `record`s ist dank des F# `option`-Typs einfach umzusetzen:

```fsharp
type Person = {
    FirstName: FirstName option
    LastName: LastName option
}
```

Hinweis: Auch wenn der `option`-Typ vielleicht auf den ersten Blick wie ein `nullable`-Typ in C# aussieht (`FirstName?`): Nein, das ist nicht das Gleiche. `option`s können miteinander kombiniert werden, sind also um einiges mächtiger als C# `nullable`s.

Eine Person kann also auch ohne Vor- und/oder Nachnamen erzeugt werden:

```fsharp
let marge = {
    FirstName = Some (FirstName "Marge")
    LastName = None
}
```

Doch wie handhabt man den `UserName` mit Validierung?

```fsharp
type Person = {
    FirstName: FirstName option
    LastName: LastName option
    UserName: UserName // ??
}
```

## Smart Constructor to the rescue

Hier kommt das "Smart Constructor" Pattern ins Spiel:

```fsharp
// `UserName` is private
type UserName = private UserName of string

module UserName =
    let isValid s = not (String.IsNullOrEmpty(s)) && s.Length < 10

    // smart constructor
    // string -> Result<UserName, string>
    let create (str: string) =
        if isValid str then
            Ok (UserName str)
        else
            Error $"invalid UserName '{str}'"

    // helper function to extract the string
    // UserName -> string
    let value (UserName str) = str
```

Der `UserName`-Typ wird mit dem `private` Attribut versehen. Somit kann der `UserName` nur innerhalb des aktuellen Scopes erzeugt werden.

Im nächsten Schritt wird ein gleichnamiges Modul erzeugt, welches eine `create`-Funktion enthält. Innerhalb dieser Funktion wird die Eingabe validiert (`isValid`). Der Rückgabewert der `create`-Funktion ist ein `Result`-Typ, welcher entweder einen `Ok UserName` oder `Error msg` enthält.

Hier ein Beispiel, wie die `create`-Funktion aufgerufen wird:

```fsharp
let maybeUserName someString = 
    match UserName.create someString with
    | Ok validName -> validName |> UserName.value
    | Error e -> e

maybeUserName "lisa" // -> "lisa"
maybeUserName ""     // -> "invalid UserName ''"
```

Wichtig: Die `create`-Funktion muss nicht zwingend einen `Result`-Typ zurückgeben wie in diesem Beispiel. Wenn die Eingabe nicht gültig ist, kann die `create`-Funktion auch ein `option`-Typ zurückgeben, eine Exception werfen oder etwas anderes machen! 

Es geht darum, dass es nie einen ungültigen `UserName` geben kann.

Wie mit dem Ergebnis der Validierung umgegangen wird, kann man selbst entscheiden.

Ob man nun einen weiteren Smart Constructor für den `Person`-Typ verwendet oder eine andere Strategie, wie beispielsweise das "Applicative Validation"-Pattern, steht einem frei.

## Beispiel

Das Erstellen eines `Person`-Typs mittels einer `tryCreatePerson` Funktion könnte z. B. folgendermaßen aussehen:

```fsharp
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

// string -> string -> string -> Result<Person, string>
let tryCreatePerson fn ln un =

    // string -> Option<FirstName>    
    let maybeFirstName fn =
        if String.IsNullOrEmpty(fn) then
            None
        else
            Some (FirstName fn)
        
    // string -> Option<LastName>
    let maybeLastName ln =
        if String.IsNullOrEmpty(ln) then
            None
        else
            Some (LastName ln)

    // string -> Result<Person, string>        
    match UserName.create un with
    | Error e ->
        Error e
    | Ok validNameUserName ->
        Ok  {
                FirstName = maybeFirstName fn
                LastName = maybeLastName ln
                UserName = validNameUserName
            }
```

## Fazit

Um sicherzustellen, dass vermeintlich einfache Typen (wie `string`) im Domänenkontext Sinn ergeben ("Optionaler Vorname", "UserName mit Regeln"), bietet es sich an, dedizierte Typen zu erstellen. 

Das "Smart Constructor" Pattern erleichtert die Validierung einfacher Typen, indem das Erstellen ungültiger Typen unterbunden wird (Stichwort: ["Prevent inpossible states (oder wie das heisst TODO Better link"](https://sporto.github.io/elm-patterns/basic/impossible-states.html)).))

Ich möchte hier auch hervorheben, dass das obige Beispiel sehr kompakt und einfach ist.
Es ist überschaubarer/wartbarer ("it fits in your head"), als das Gegenstück in C#.

Dies ist eine freie Übersetzung eines privaten Blog-Posts von mir: https://draptik.github.io/posts/2020/02/10/fsharp-smart-constructor/

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
            Some (UserName str)
        else
            None

    // helper function extracting value
    let value (UserName s) = s
```

Anwendung:

```fsharp
let workingWithSmartCtor name =
    let maybeValidUserName = UserName.create name
    match maybeValidUserName with
    | Some validName -> validName |> UserName.value
    | None -> "invalid UserName"

workingWithSmartCtor ""     // -> "invalid UserName"
workingWithSmartCtor "lisa" // -> "lisa"
```

## Artikel

In diesem Artikel moechte ich zeigen, wie man F# `record`s mit eingebauter Validierung erzeugt.

Beginnen wir mit einem einfachem `Person` Typ:

```fsharp
type Person = {
    FirstName: string
    LastName: string
}
```

Dies ist ein `record` mit einem `FirstName`, einem `LastName` und einem `UserName` (Anmerkung: Ab C# Version 9 (TODO: Check Version number) gibt es das Konzept von `record`s auch in C#). Um eine Instanz dieses Typs zu erzeugen:

```fsharp
let homer = {
    FirstName = "Homer"
    LastName = "Simpson"
}
```

Moechte man nun eine Formatierungfunktion anbieten, die mit dem Vor- und Nachnamen arbeitet, koennte eine einfache Implementierung folgendermassen aussehen:

```fshapr
let formatName firstName lastName = $"{lastName}, {firstName}"
```

Da es sich beim Vor- und Nachnamen um einen `string` handelt, besteht eine gewisse Verwechselungsgefahr:

```fsharp
let formattedName = formatName "Simpson" "Homer"
```

Dieses Problem wird auch als "Primitive Obsession" bezeichnet: Anstatt das vorhandene Typ-System zu nutzen, wird ein `string` verwendet. Abhilfe schafft die Verwendung dedizierter Typen fuer Vor- und Nachnamen:

```fsharp
type FirstName = FirstName of string
type LastName = LastName of string
```

Im Gegensatz zum `Person`-Typ handelt es sich hier um sogenannte Single-Case Discriminated Unions.

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

Der `formatName`-Aufruf ist kann nun ohne Verwechselungsgefahr der Parameterreihenfolge erfolgen, da eine falsche Reihenfolge vom Compiler unterbunden wird:

```fsharp
let formattedName1 = formatName bart.LastName bart.FirstName
let formattedName2 = formatName (FirstName "Bart") (LastName "Simpson")
let formattedName3 = formatName (LastName "Simpson") (FirstName "Bart") // kompiliet nicht
```

Szenenwechsel: Aus unerklaerlichen Gründen aendern sich die Anforderungen fuer den `Person`-Typ. Die Angabe eines Vor- und Nachnamen sind nun optional, dafuer muss ein `UserName` vorhanden sein. Der `UserName` hat zusaetzlich die Anforderung, dass er (a) nicht leer sein darf, und (b) nicht mehr als 10 Zeichen lang sein darf.

Die Umsetzung von optionalen Eigenschaften `record`s ist dank des F# `option`-Typs einfach umzusetzen:

```fsharp
type Person = {
    FirstName: FirstName option
    LastName: LastName option
}
```

Eine Person kann also auch ohne Vor- und/oder Nachnamen erzeugt werden:

```fsharp
let marge = {
    FirstName = Some (FirstName "Marge")
    LastName = None
}
```

Doch wie handhaben wir einen `UserName`, mit Validierung?

```fsharp
type Person = {
    FirstName: FirstName option
    LastName: LastName option
    UserName: UserName // ??
}
```

Hier kommt das "Smart Constructor" Pattern ins Spiel:

```fsharp
type UserName = private UserName of string

module UserName =
    let isValid s = not (String.IsNullOrEmpty(s)) && s.Length < 50

    // smart constructor
    let create (str: string) =
        if isValid str then
            Some (UserName str)
        else
            None

    // helper function to extract the string
    let value (UserName str) = str
```

Der `UserName`-Typ wird mit dem `private` Attribut versehen. Somit kann der `UserName` nur innerhalb des folgenden Moduls erzeugt werden.

Im naechsten Schritt wird ein gleichnamiges Modul erzeugt, welches eine `create`-Funktion enthaelt. Innerhalb dieser Funktion wird die Eingabe validiert (`isValid`). Der Rueckgabewert der `create`-Funktion ist ein `option`-Typ, welcher entweder einen `Some UserName` oder `None` enthaelt.

Hier ein Beispiel, wie die `create`-Funktion aufgerufen wird:

```fsharp
let maybeUserName = 
    match UserName.create someString with
    | Some validName -> validName |> UserName.value
    | None -> "invalid UserName"

maybeUserName "lisa" // -> "lisa"
maybeUserName ""     // -> "invalid UserName"
```

Wichtig: Die `create`-Funktion muss nicht zwingend einen `option`-Typ zurueckgeben, wie in diesem Beispiel. Wenn die Eingabe nicht gültig ist, kann die `create`-Funktion auch eine Exception werfen, oder etwas anderes machen! Es geht lediglich darum, dass die Eingabe validiert wird: Wie mit dem Ergebnis der Validierung umgegangen wird, kann man selbst entscheiden.

Das Erstellen eines `Person`-Typs koennte z.B. folgendermassen aussehen:

```fsharp
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
    let maybeUserName = 
        match UserName.create un with
        | Some validNameUserName -> Some { FirstName = FirstName fn; LastName = LastName ln; UserName = validNameUserName }
        | None -> None

let maybeUser1 = tryCreatePerson "Lisa" "Simpson" "lisa rocks"
let maybeUser2 = tryCreatePerson "Homer" "Simpson" ""
let maybeUser3 = tryCreatePerson "Marge" "Simpson" "lisa rocks"
```

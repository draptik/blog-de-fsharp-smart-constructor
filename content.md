# F# Smart Constructor

<!-- [Sebastian] An welche Leserschaft richtet sich der Artikel? Der Artiekl setzt zwar nicht zwingend F#-Kenntnisse voraus, geht allerdings auf einige F#-Punkte nur kurz oder gar nicht ein. Ich vermute, grundlegende F#-Kenntnisse sollte der Leser mitbringen, richtig?  -->

Das "F# Smart Constructor" Pattern ermöglicht die Erzeugung von validierten F# Typen.

<!-- [Sebastian] Was ist ist Motivation, hier F# zu verwenden? Lässt sich mit F# das Pattern besonders gut zeigen? Oder ist es in F# besonders wichtig, dieses Pattern zu verwenden? Oder lässt sich die Validierung - anders als in C# - in F# besonders gut umsetzen? -->

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

<!-- [Sebastian] Woran kann man erkennen, dass dies hier eine Instanz des Typs `Person` ist? Für mich sieht das aus wie ein typenloser Adhoc-Record. -->

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

<!-- [Sebastian] Guter Hinweis, denn hier ist für Non F# Reader eine kleine Erläuterung der sonderbar aussehenden Syntax nötig (ohne auch Currying eingehen zu müssen). Für einen Python-Leser zum Beispiel sieht das oben aus wie eine Zuweisung an 3 Variablen. -->

Hinweis: Der Kommentar `// string -> string -> string` beschreibt die Typsignatur der darunterstehenden Funktion. Da F# eine bessere Typinferenz als C# hat, muss man meist die Typen nicht explizit angeben. Für diesen Artikel habe ich an manchen Stellen die Signatur als Kommentar hinzugefügt um Unklarheiten zu vermeiden. Der letzte Wert in der Signatur ist der Rückgabewert der Funktion. <!-- [Sebastian] Du meinst hier nicht den Wert, sondern den Typ in der Signatur, richtig? --> Alle anderen Werte sind Parameter der Funktion. <!-- [Sebastian] Das könnte man jetzt so verstehen, als wäre `lastName` der Rückgabewert, was er ja nicht ist, richtig? Der Rückgabewert der Funktion ist der Wert des letztens Ausdrucks - wobei es hier in der Funktion nur einen Ausdruck gibt. --> In C# wäre die entsprechende Signatur `Func<string, string, string>`.

Da es sich beim Vor- und Nachnamen um einen `string` handelt, besteht eine gewisse Verwechslungsgefahr:

```fsharp
// Achtung: Vor- und Nachname sind vertauscht:
let formattedName = formatName "Simpson" "Homer"
```

<!-- [Sebastian] Kurzer Hinweis, dass das hier die Anwendung der zuvor definierten Funktion `formatName` ist? C#- und Python3-Leser vermissen hier die Klammern - Python2-Leser kommen damit zurecht. -->

Dieses Problem wird auch als ["Primitive Obsession"](https://wiki.c2.com/?PrimitiveObsession) Antipattern bezeichnet: Anstatt das vorhandene Typ-System zu nutzen, wird ein `string` verwendet. Abhilfe schafft die Verwendung dedizierter Typen für Vor- und Nachname:

<!-- [Sebastian] In Python gibt es dazu *Named Parameters*, hier zählt nur der Name des Parameters, nicht seine Position in der Liste der Argumente. In F#-Sytnax sähe das dann so aus:
 
```fsharp
let formattedName = formatName firstName="Simpson" lastName="Homer"
``` 
 -->

```fsharp
type FirstName = FirstName of string
type LastName = LastName of string
```

Im Gegensatz zum `Person`-Typ handelt es sich hier um sogenannte [Single-Case Discriminated Unions](https://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/).

<!-- [Sebastian] Was ist hier genau eine *Union*? Den Begriff *Wrapper* aus dem verlinkten Artikel verstehe ich noch. -->

Der `Person`-Typ kann nun diese dedizierten Typen verwenden:

```fsharp
type Person = {
    FirstName: FirstName
    LastName: LastName
}
```

<!-- [Sebastian] Und hier fängt mein Hirn an auszusteigen, zumal der der Ansatz ja oben versprochen hat, die Verwechslungsgefahr zu reduzieren. Ich sehe jetzt zwei Felder und zwei Typen, wobei Feld und Typ den gleichen Namen tragen. :-o Ist das übliche F#-Praxis? In anderen Sprachen würde man statt `type FirstName` vermutlich eher `type FirstNameType` schreiben, um diese Verwechslung zu vermeiden? -->

Um nun eine Instanz dieses Typs zu erzeugen, muss der jeweilige `string` erst in den entsprechenden `FirstName`- und `LastName`-Typen umgewandelt werden:

```fsharp
let bart = {
    FirstName = FirstName "Bart"
    LastName = LastName "Simpson"
}
```

<!-- [Sebastian] Dieses Zeilen oben sehen für mich jetzt kurios aus, `FirstName` auf der linken und auf der rechten Seite des Zuweisungsoperators. -->

Der `formatName`-Aufruf kann nun ohne Verwechslungsgefahr der Parameterreihenfolge erfolgen, da eine falsche Reihenfolge vom Compiler unterbunden wird:

```fsharp
let formattedName1 = formatName bart.LastName bart.FirstName // -> "Simpson, Bart"
let formattedName2 = formatName (FirstName "Bart") (LastName "Simpson") // -> "Simpson, Bart"
let formattedName3 = formatName (LastName "Simpson") (FirstName "Bart") // 😡 kompiliert nicht
```

<!-- [Sebastian] Frage am Rande: Ist das Problem nicht eher ein gestelltes? Sollte die Funktion `formatName` nicht besser den gesamten Record übergeben bekommen und sich selber herauspicken, was sie braucht? Nun, ich verstehe, es geht hier um Lehrzwecke... Die Funktion könnte ja auch zwei Records übergeben bekommen und dann hätten wir das gleichen Problem wieder. Was mich auf eine interessante Folgefrage bringt. Die beiden `string`-Parameter wrappen, verstehe ich, gekauft. Würde man auf ähnliche Weise zwei ganze `Person`-Records mappen und dafür dann zwei Wrapper-Typen `Husband` und `Wife` definieren? --->

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

<!-- [Sebastian] Könntest Du auf den `option`-Type noch etwas genauer eingehen? Ich bin hier gestolpert: "Hm, das ist echt ein Typ?" `option` ist in F# wohl wirklich ein eigener Typ, dem ein Typ zugrundeliegt und Werte von eben diesem Typ kann so ein `option`-Wert annehmen oder eben keinen Wert.` Und ja, für mich klingt das sehr nach einem `nullable`-Typ in C#. Inwiefern `option` jetzt mächtiger ist, erschließt sich mir auf die Schnelle nicht - ist vermutlich hier aber auch nicht von Belang, oder? -->

Eine Person kann also auch ohne Vor- und/oder Nachnamen erzeugt werden:

```fsharp
let marge = {
    FirstName = Some (FirstName "Marge")
    LastName = None
}
```

<!-- [Sebastian] Könntest Du zu dem `Some` noch kurz was sagen? Vielleicht die Deklaration von `option` dazu schreiben, dann wären `None` und `Some` gleichermaßen kurz erläuert. Oder setzt Du diese Kenntnisse voraus? -->

Doch wie handhabt man den `UserName` mit Validierung?

```fsharp
type Person = {
    FirstName: FirstName option
    LastName: LastName option
    UserName: UserName // ??
}
```

<!-- [Sebastian] Wie sieht die Deklartion des Tytps `UserName` aus? Das wäre hier insofern wichtig zu sehen, weil Du ja vermutlich darauf rauswillst, dass die beiden Constraints zum Typ `UserName` im `Person`-Records aufgehiben werden sollen, nicht aber im Typ `UserName`, richtig? Wobei sich die Frage stellt... warum eigentlich im `Person`-Record und nicht im `UserName`-Typ? :-o -->

<!-- [Sebastian] Bis hierhin bin ich heute gekommen. To be continued... :-) -->

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

Ich möchte hier auch hervorheben, dass das obiges Beispiel sehr kompakt und einfach ist.
Es ist überschaubarer/wartbarer (["it fits in your head"](https://blog.ploeh.dk/2021/06/14/new-book-code-that-fits-in-your-head/)), als das Gegenstück in C#.

## Resources

- Dies ist eine freie Übersetzung eines privaten Blog-Posts von mir: https://draptik.github.io/posts/2020/02/10/fsharp-smart-constructor/
- Code, zum Rumspielen, und Tests, gibts hier: [Demo](Demo)

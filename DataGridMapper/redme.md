# MC.Data.DataGrid

MC.Data.DataGrid to biblioteka rozszerzająca standardowy WinForms DataGridView o trzy główne obszary funkcjonalności:
- GridProperty — deklaratywna konfiguracja kolumn na podstawie atrybutów modelu oraz obsługa akcji dla kolumn typu Button.
- GridFilter — filtrowanie danych bezpośrednio powiązane z DataGridView, obsługujące zarówno źródła IEnumerable, jak i IQueryable.
- CustomColumn — możliwość wyświetlania złożonych widoków WinForms jako zawartości pojedynczej komórki, z renderowaniem do Bitmap i pamięciowym cache LRU.

Biblioteka została zaprojektowana tak, aby standardowy scenariusz użycia był prosty:

                         MC.Data.DataGrid
                                │
              ┌─────────────────┼─────────────────┐
              │                 │                 │
              ▼                 ▼                 ▼
        GridProperty        GridFilter       CustomColumn
              │                 │                 │
              ▼                 ▼                 ▼
        konfiguracja        filtrowanie       własny widok
        kolumn              danych            WinForms
              │                 │                 │
              └─────────────────┼─────────────────┘
                                │
                                ▼
                         DataGridView

## 1. Trzy główne elementy biblioteki

### 1.1. GridProperty

GridProperty odpowiada za konfigurację DataGridView na podstawie modelu danych.

Pozwala między innymi:
- definiować kolumny za pomocą GridViewAttribute,
- określać nazwy i widoczność kolumn,
- definiować typ kolumny,
- określać formatowanie,
- włączać edycję,
- tworzyć kolumny przycisków,
- obsługiwać akcje GridAction<T>,
- korzystać z CustomColumn.

Typowy przepływ:

Model
  │
  │ [GridView(...)]
  ▼
GridProperty.InitData<T>()
  │
  ▼
DataGridView

1.2. GridFilter
GridFilter odpowiada za filtrowanie danych prezentowanych przez DataGridView.

Obsługuje między innymi:

porównania wartości,

tekst Contains,

StartsWith,

EndsWith,

operatory <, <=, >, >=,

null,

filtrowanie wielu właściwości jednocześnie.

Mechanizm rozpoznaje źródła danych typu:

IEnumerable

oraz:

IQueryable

Dzięki temu możliwe jest filtrowanie zarówno danych znajdujących się w pamięci:

List<Product>

jak i zapytań:

IQueryable<Product>

Przepływ:

DataGridView
     │
     ▼
 GridFilter
     │
     ▼
FilterDefinition
     │
     ▼
FilterExpressionBuilder
     │
     ├── IEnumerable → LINQ to Objects
     │
     └── IQueryable → Expression / LINQ Provider

1.3. CustomColumn
CustomColumn odpowiada za renderowanie własnego widoku WinForms jako zawartości komórki DataGridView.

Jest przeznaczony dla sytuacji, w których standardowe kolumny DataGridView nie są wystarczające.

Przykładowo pojedyncza komórka może prezentować:

┌──────────────────────────────┐
│ Jan Kowalski                 │
│ ul. Piotrkowska 10           │
│ Pabianice                    │
│ ● Aktywny                    │
└──────────────────────────────┘

Widok jest renderowany do Bitmap, a wynik jest przechowywany w BitmapCache.

Przepływ:

TData
  │
  ▼
TView
  │
  ▼
AncherRenderHost
  │
  ▼
Bitmap
  │
  ▼
BitmapCache
  │
  ▼
DataGridView

Najważniejszą zasadą jest to, że nie tworzona jest osobna kontrolka dla każdej komórki.

Jedna instancja TView jest współdzielona przez CustomColumn.

2. Typowy scenariusz użycia
Najczęstszy scenariusz może łączyć wszystkie trzy elementy:

                         Model
                           │
             ┌─────────────┼─────────────┐
             │             │             │
             ▼             ▼             ▼
        GridProperty   GridFilter   CustomColumn
             │             │             │
             │             │             │
             └─────────────┼─────────────┘
                           │
                           ▼
                      DataGridView

Przykładowo aplikacja może:

skonfigurować kolumny przez GridViewAttribute,

wyświetlić własny PersonView jako CustomColumn,

udostępnić przycisk Edit,

pozwolić użytkownikowi filtrować Name, Age oraz IsActive.

3. GridProperty
3.1. Podstawowa konfiguracja
Najprostszy scenariusz:

private readonly GridProperty _gridProperty;

public Form1()
{
    InitializeComponent();

    _gridProperty =
        new GridProperty(dataGridView1);

    _gridProperty.InitData<Product>();

    dataGridView1.DataSource = products;
}

Konfiguracja kolumn znajduje się w modelu.

public class Product
{
    [GridView("Nazwa")]
    public string Name { get; set; }

    [GridView(
        "Cena",
        columnType: GridViewAttribute.EColumnType.Number)]
    public decimal Price { get; set; }
}

4. GridViewAttribute
GridViewAttribute określa sposób prezentacji właściwości modelu w DataGridView.

Przykład:

[GridView("Nazwa")]
public string Name { get; set; }

Atrybut jest przeznaczony do właściwości modelu.

Konstruktor
GridViewAttribute(
    string name,
    bool ignore = false,
    bool visibility = true,
    bool allowEdit = false,
    string columnName = "",
    string format = "",
    EColumnType columnType = EColumnType.None,
    string actionName = "")

Parametry
Parametr	Znaczenie
name	Tekst nagłówka kolumny
ignore	Całkowite pominięcie właściwości
visibility	Widoczność kolumny
allowEdit	Możliwość edycji
columnName	Nazwa kolumny DataGridView
format	Format prezentowania wartości
columnType	Typ kolumny
actionName	Nazwa akcji dla kolumny Button

5. Typy kolumn
Dostępne są:

None
Text
Number
Date
Time
DateTime
Button
Tooltip
MemoEdit
Boolean
CustomColumn

None
Standardowa kolumna.

[GridView("Nazwa")]
public string Name { get; set; }

Text
Kolumna tekstowa.

[GridView(
    "Nazwa",
    columnType: GridViewAttribute.EColumnType.Text)]
public string Name { get; set; }

Number
Kolumna dla wartości liczbowych.

Obsługiwane typy obejmują:

byte
sbyte
short
ushort
int
uint
long
ulong
float
double
decimal

Przykład:

[GridView(
    "Cena",
    columnType: GridViewAttribute.EColumnType.Number)]
public decimal Price { get; set; }

Domyślny format:

N2

Można go zmienić:

[GridView(
    "Cena",
    format: "N0",
    columnType: GridViewAttribute.EColumnType.Number)]
public decimal Price { get; set; }

Boolean
Tworzy kolumnę checkbox.

Obsługiwane:

bool
bool?

Przykład:

[GridView(
    "Aktywny",
    columnType: GridViewAttribute.EColumnType.Boolean)]
public bool IsActive { get; set; }

Date
Kolumna dla DateTime.

Domyślny format:

dd.MM.yyyy

Przykład:

[GridView(
    "Data",
    columnType: GridViewAttribute.EColumnType.Date)]
public DateTime CreatedAt { get; set; }

Własny format:

[GridView(
    "Data",
    format: "yyyy-MM-dd",
    columnType: GridViewAttribute.EColumnType.Date)]
public DateTime CreatedAt { get; set; }

Time
Obsługuje:

TimeSpan
TimeSpan?
DateTime
DateTime?

Przykład:

[GridView(
    "Czas",
    columnType: GridViewAttribute.EColumnType.Time)]
public TimeSpan Duration { get; set; }

DateTime
Kolumna dla daty i czasu.

Domyślny format:

dd.MM.yyyy HH:mm:ss

Przykład:

[GridView(
    "Utworzono",
    columnType: GridViewAttribute.EColumnType.DateTime)]
public DateTime CreatedAt { get; set; }

Tooltip
Wartość komórki może być prezentowana jako tooltip.

[GridView(
    "Informacja",
    columnType: GridViewAttribute.EColumnType.Tooltip)]
public string Information { get; set; }

MemoEdit
Kolumna przeznaczona dla dłuższego tekstu.

[GridView(
    "Opis",
    columnType: GridViewAttribute.EColumnType.MemoEdit)]
public string Description { get; set; }

Button
Tworzy przycisk.

Wymagane jest actionName.

[GridView(
    "Edytuj",
    columnType: GridViewAttribute.EColumnType.Button,
    actionName: "Edit")]
public string Edit { get; set; }

Następnie:

_gridProperty
    .GetAction<Product>("Edit")
    .Click += Edit_Click;

6. Visibility
Domyślnie kolumna jest widoczna:

[GridView("Nazwa")]
public string Name { get; set; }

Można ją ukryć:

[GridView(
    "Wartość techniczna",
    visibility: false)]
public string TechnicalValue { get; set; }

Kolumna pozostaje skonfigurowana, ale nie jest widoczna.

7. Ignore
Właściwość można całkowicie pominąć:

[GridView(
    "Wartość ignorowana",
    ignore: true)]
public string IgnoredValue { get; set; }

Różnica:

visibility: false
    → kolumna istnieje, ale jest ukryta

ignore: true
    → właściwość jest pomijana podczas konfiguracji

8. AllowEdit
Domyślnie kolumny są tylko do odczytu.

[GridView(
    "Nazwa",
    allowEdit: true)]
public string Name { get; set; }

Dla dłuższego tekstu:

[GridView(
    "Opis",
    allowEdit: true,
    columnType: GridViewAttribute.EColumnType.MemoEdit)]
public string Description { get; set; }

9. ColumnName
name określa tekst nagłówka, natomiast columnName nazwę kolumny DataGridView.

[GridView(
    "Nazwa użytkownika",
    columnName: "UserName")]
public string Name { get; set; }

10. Format
Format określa sposób prezentowania wartości.

[GridView(
    "Cena",
    format: "N2",
    columnType: GridViewAttribute.EColumnType.Number)]
public decimal Price { get; set; }

Dla daty:

[GridView(
    "Data",
    format: "yyyy-MM-dd",
    columnType: GridViewAttribute.EColumnType.Date)]
public DateTime CreatedAt { get; set; }

Format jest zgodny ze standardowymi formatami .NET odpowiednimi dla danego typu danych.

11. Akcje GridAction
Dla kolumn Button można pobrać akcję za pomocą:

GridAction<T> GetAction<T>(string key)

Przykład:

_gridProperty
    .GetAction<Product>("Edit")
    .Click += Edit_Click;

key musi odpowiadać actionName:

[GridView(
    "Edytuj",
    columnType: GridViewAttribute.EColumnType.Button,
    actionName: "Edit")]

12. GridAction<T>
GridAction<T> reprezentuje akcję przypisaną do przycisku.

Publiczne elementy:

string Key

oraz:

event EventHandler<GridActionEventArgs<T>> Click

13. GridActionEventArgs<T>
Argument zdarzenia zawiera:

Item
Grid
RowIndex
ColumnIndex

Przykład:

private void Edit_Click(
    object sender,
    GridActionEventArgs<Product> e)
{
    Product product = e.Item;

    MessageBox.Show(product.Name);
}

Item jest najważniejszą właściwością — zawiera obiekt modelu związany z klikniętym wierszem.

14. GridFilter
GridFilter jest opcjonalnym elementem biblioteki.

Jego zadaniem jest dostarczenie użytkownikowi interfejsu filtrowania danych prezentowanych przez DataGridView.

Przykładowe użycie:

private readonly GridFilter _gridFilter;

public Form1()
{
    InitializeComponent();

    _gridFilter =
        new GridFilter(dataGridView1);

    _gridFilter.Enable();
}

Filtrowanie wykorzystuje definicje:

FilterDefinition

oraz:

FilterOperator

i buduje odpowiednie wyrażenie:

FilterEditor
      │
      ▼
FilterDefinition
      │
      ▼
FilterExpressionBuilder
      │
      ▼
IDataSourceAdapter
      │
      ├── EnumerableDataSourceAdapter<T>
      │
      └── QueryableDataSourceAdapter<T>

15. FilterOperator
Dostępne operatory:

Equals
NotEquals
Contains
StartsWith
EndsWith
GreaterThan
GreaterThanOrEqual
LessThan
LessThanOrEqual
IsNull
IsNotNull

Equals
Age == 18

NotEquals
Age != 18

Contains
Name.Contains("Jan")

StartsWith
Name.StartsWith("Jan")

EndsWith
Name.EndsWith("ski")

GreaterThan
Age > 18

GreaterThanOrEqual
Age >= 18

LessThan
Age < 18

LessThanOrEqual
Age <= 18

IsNull
Name == null

IsNotNull
Name != null

16. FilterDefinition
FilterDefinition opisuje pojedynczy warunek.

new FilterDefinition(
    "Name",
    FilterOperator.Contains,
    "Jan");

Posiada:

string PropertyName
FilterOperator Operator
object Value

Przykład:

var filter =
    new FilterDefinition(
        "Price",
        FilterOperator.GreaterThan,
        100m);

17. Filtrowanie wielu właściwości
Jeżeli istnieje kilka aktywnych filtrów, są one łączone operatorem logicznym AND.

Przykładowo:

Name contains "Jan"
AND
Age >= 18
AND
IsActive == true

jest reprezentowane jako jedno wyrażenie:

Name.Contains("Jan")
    &&
Age >= 18
    &&
IsActive == true

18. IEnumerable i IQueryable
Biblioteka rozróżnia dwa podstawowe rodzaje źródeł.

IEnumerable
Przykład:

List<Product> products;

Filtrowanie wykonywane jest przez:

EnumerableDataSourceAdapter<Product>

i LINQ to Objects.

Schemat:

IEnumerable<Product>
       │
       ▼
EnumerableDataSourceAdapter<Product>
       │
       ▼
Expression.Compile()
       │
       ▼
Where(...)

IQueryable
Przykład:

IQueryable<Product> products;

Filtrowanie wykonywane jest przez:

QueryableDataSourceAdapter<Product>

Schemat:

IQueryable<Product>
       │
       ▼
QueryableDataSourceAdapter<Product>
       │
       ▼
Expression<Func<Product, bool>>
       │
       ▼
Queryable.Where(...)

Pozwala to pozostawić wyrażenie jako expression tree dla dostawcy IQueryable.

19. BindingSource
GridFilter może pracować również ze źródłem opakowanym w BindingSource.

BindingSourceResolver rozwiązuje zagnieżdżone BindingSource.

Przykład:

BindingSource
     │
     ▼
BindingSource
     │
     ▼
List<Product>

ostatecznie prowadzi do:

List<Product>

Dzięki temu filtr nie musi być bezpośrednio podłączony do końcowego źródła danych.

20. CustomColumn
CustomColumn<TData, TView> służy do wyświetlania złożonego widoku WinForms w pojedynczej komórce.

Przykładowy model:

public class Person : ICustomColumnData
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public bool IsActive { get; set; }
}

Widok:

public class PersonView :
    UserControl,
    IGridView<Person>
{
    public void SetData(Person data)
    {
        // przygotowanie widoku
    }
}

21. ICustomColumnData
Model używany przez CustomColumn musi implementować:

public interface ICustomColumnData
{
    int Id { get; set; }
}

Id jest wykorzystywane jako podstawowy element klucza cache.

Przykład:

public class Person : ICustomColumnData
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }
}

Założenie:

Id → jednoznaczna identyfikacja reprezentacji wizualnej danych

22. IGridView<TData>
Widok TView musi:

dziedziczyć po Control,

implementować IGridView<TData>.

Przykład:

public class PersonView :
    UserControl,
    IGridView<Person>
{
    public void SetData(Person data)
    {
        nameLabel.Text = data.Name;
        addressLabel.Text = data.Address;
    }
}

SetData() przygotowuje współdzielony widok do renderowania konkretnego obiektu.

23. Współdzielony TView
CustomColumn posiada jedną instancję TView.

Model:

CustomColumn
      │
      └── TView

a nie:

CustomColumn
      │
      ├── TView #1
      ├── TView #2
      ├── TView #3
      └── TView #4

Dla dużej liczby wierszy ma to istotne znaczenie.

Przykład:

1000 wierszy
1000 komórek CustomColumn

             ↓

1 współdzielony TView
1 BitmapCache

zamiast:

1000 wierszy

             ↓

1000 instancji TView

24. TView jako renderer
Współdzielony TView należy traktować jako tymczasowy renderer.

Przepływ:

TView
  │
  ▼
SetData(Person 1)
  │
  ▼
Render
  │
  ▼
Bitmap

następnie:

TView
  │
  ▼
SetData(Person 2)
  │
  ▼
Render
  │
  ▼
Bitmap

Stan TView nie jest stanem konkretnej komórki.

Wynikiem pracy renderera jest bitmapa znajdująca się w cache.

25. AncherRenderHost
AncherRenderHost jest odpowiedzialny za faktyczne renderowanie TView do Bitmap.

Architektura rozdziela odpowiedzialności:

CustomCell
    │
    │ kiedy renderować?
    ▼
AncherRenderHost
    │
    │ jak wyrenderować?
    ▼
Bitmap

AncherRenderHost odpowiada za:

hostowanie TView,

ustawienie rozmiaru,

wykonanie layoutu,

przygotowanie kontrolki,

wykonanie DrawToBitmap(),

zwrócenie Bitmap.

Przepływ:

CustomCell
     │
     ▼
AncherRenderHost.Render(...)
     │
     ├── attach TView
     ├── Size
     ├── Layout
     ├── DrawToBitmap()
     │
     ▼
  Bitmap

Dlatego ControlRenderHost / AncherRenderHost jest wymaganym elementem infrastruktury CustomColumn.

26. Inicjalizacja CustomColumn
Jeżeli GridProperty korzysta z CustomColumn, należy utworzyć również host renderowania.

Przykład:

private readonly ControlRenderHost _renderHost;
private readonly GridProperty _gridProperty;

public Form1()
{
    InitializeComponent();

    _renderHost =
        new ControlRenderHost();

    _gridProperty =
        new GridProperty(
            dataGridView1,
            _renderHost);
}

W zależności od wersji biblioteki konkretna nazwa klasy hosta może być ControlRenderHost lub AncherRenderHost.

27. Przepływ CustomCell.Paint()
Najważniejszym miejscem działania CustomColumn jest CustomCell.Paint().

Przepływ:

Paint()
   │
   ├── pobranie CustomColumn
   │
   ├── standardowe elementy DataGridView
   │
   ├── pobranie TData
   │
   ├── utworzenie BitmapCacheKey
   │
   └── Cache.TryGet()
             │
             ├── HIT
             │    │
             │    └── DrawImage()
             │
             └── MISS
                  │
                  ├── View.SetData(data)
                  │
                  ├── RenderHost.Render(View)
                  │
                  ├── Cache.Set(bitmap)
                  │
                  ├── Cache.TryGet()
                  │
                  └── DrawImage()

28. Cache HIT
Jeżeli obraz znajduje się w cache:

column.Cache.TryGet(key, out bitmap)

jest wykonywane tylko:

Cache
 ↓
Bitmap
 ↓
DrawImage()

Nie wykonujemy:

View.SetData(...)

ani:

RenderHost.Render(...)

Jest to podstawowa optymalizacja mechanizmu.

29. Cache MISS
Jeżeli obrazu nie ma w cache:

Cache.TryGet()
      │
      ▼
    MISS
      │
      ▼
View.SetData(data)
      │
      ▼
RenderHost.Render(...)
      │
      ▼
Bitmap
      │
      ▼
Cache.Set()

Po zapisaniu bitmapy do cache pobierana jest niezależna instancja bitmapy:

Cache
  │
  ▼
byte[]
  │
  ▼
Bitmap
  │
  ▼
DrawImage()

30. BitmapCache
BitmapCache przechowuje wyniki renderowania.

Cache nie przechowuje bezpośrednio:

Bitmap

lecz dane obrazu:

byte[]

Schemat:

Bitmap
   │
   ▼
Encode
   │
   ▼
byte[]
   │
   ▼
BitmapCache

Przy odczycie:

BitmapCache
   │
   ▼
byte[]
   │
   ▼
Decode
   │
   ▼
Bitmap

31. Dlaczego cache przechowuje byte[]?
Dzięki temu cache nie przechowuje bezpośrednio obiektów GDI+.

Bitmap zwrócona przez TryGet() jest tymczasowa i powinna zostać zwolniona przez kod, który jej używa.

Przykład:

if (column.Cache.TryGet(key, out Bitmap bitmap))
{
    using (bitmap)
    {
        graphics.DrawImage(
            bitmap,
            cellBounds);
    }
}

32. LRU Cache
BitmapCache wykorzystuje mechanizm:

LRU
Least Recently Used

Po użyciu element jest przesuwany na początek listy.

Jeżeli przekroczony zostanie limit pamięci, usuwane są najmniej używane wpisy.

MOST RECENT
    │
    ▼
[Entry A]
[Entry B]
[Entry C]
[Entry D]
    │
    ▼
LEAST RECENT

Przy przekroczeniu limitu usuwany jest wpis z końca.

33. Limit pamięci
Cache posiada limit pamięci określany przez MaxMemoryMB.

Przykład:

new BitmapCache(20)

oznacza limit około:

20 MB

zakodowanych danych obrazu.

Jeżeli pojedynczy obraz przekracza cały dostępny limit, może zostać pominięty:

image > MaxMemoryBytes
        │
        ▼
   nie zapisuj

34. BitmapCacheKey
Klucz cache identyfikuje konkretną wersję obrazu.

Przykładowo:

new BitmapCacheKey(
    data.Id,
    cellBounds.Size,
    ImageFormat.Png,
    90L);

Wynik renderowania zależy więc od:

Id
Size
ImageFormat
Quality

Przykładowo ten sam obiekt może posiadać:

Id = 10
Size = 200x80

oraz:

Id = 10
Size = 300x100

jako dwa niezależne wpisy cache.

35. Zmiana danych a cache
Id identyfikuje dane w cache.

Jeżeli:

Id = 10

pozostaje takie samo, ale zmieniają się dane wizualne obiektu, istnieje możliwość użycia starej bitmapy.

Dlatego należy stosować jedną z zasad:

Dane niemutowalne
Po utworzeniu obiektu jego dane wizualne nie zmieniają się.

Unieważnianie cache
Po zmianie danych usuwany jest odpowiedni wpis cache.

Wersjonowanie
Klucz można rozszerzyć:

Id + Version + Size + Format + Quality

36. Paint() i wydajność
Paint() może być wywoływany wielokrotnie przez WinForms.

Nie należy wykonywać w nim:

operacji biznesowych,

zapisu danych,

długotrwałego I/O,

zapytań do bazy danych,

ładowania danych z zewnętrznych źródeł.

Przykładowo przewijanie DataGridView może powodować wielokrotne wywołanie Paint().

Dlatego:

CACHE HIT
    ↓
DrawImage()

powinien być szybki.

37. Renderowanie tylko przy CACHE MISS
Podstawowa zasada CustomColumn:

CACHE HIT
    │
    ├── brak SetData()
    ├── brak Render()
    └── DrawImage()

oraz:

CACHE MISS
    │
    ├── SetData()
    ├── Render()
    ├── Cache.Set()
    └── DrawImage()

Koszt renderowania jest więc ponoszony przede wszystkim podczas pierwszego wygenerowania konkretnej wersji obrazu.

38. Cykl życia Bitmap
Renderowanie
TView
  │
  ▼
RenderHost
  │
  ▼
Bitmap rendered
  │
  ▼
BitmapCache.Set()
  │
  ▼
Encode
  │
  ▼
byte[]
  │
  ▼
Cache

Bitmapa rendered może zostać następnie zwolniona.

Wyświetlanie
Cache
  │
  ▼
byte[]
  │
  ▼
Decode
  │
  ▼
Bitmap
  │
  ▼
Graphics.DrawImage()
  │
  ▼
Dispose()

39. Odpowiedzialności komponentów CustomColumn
CustomColumn<TData, TView>
Odpowiada za:

konfigurację kolumny,

współdzielony TView,

AncherRenderHost,

BitmapCache,

opcjonalny KeySelector.

CustomCell<TData, TView>
Odpowiada za:

Paint(),

pobranie danych,

utworzenie BitmapCacheKey,

sprawdzenie cache,

uruchomienie renderowania przy MISS,

narysowanie bitmapy.

TView
Odpowiada za:

prezentację TData,

przygotowanie stanu przez SetData(),

wygląd widoku,

działanie jako zwykła kontrolka WinForms.

AncherRenderHost
Odpowiada wyłącznie za:

hostowanie TView,

rozmiar,

layout,

renderowanie do Bitmap.

BitmapCache
Odpowiada za:

przechowywanie wyników renderowania,

LRU,

limit pamięci,

kodowanie bitmap,

dekodowanie bitmap.

40. Współbieżność CustomColumn
Współdzielony TView oznacza, że renderer nie powinien być używany równolegle.

TView jest kontrolką WinForms, a jego stan jest zmieniany przez:

View.SetData(data);

Dlatego standardowy renderowanie miałoby zostać przeniesione do wielu wątków, potrzebna byłaby zmiana architektury lub osobne instancje renderer model:

UI thread
    │
    ▼
Paint()
    │
    ▼
SetData()
    │
    ▼
Render()

jest zgodny z założeniem architektury.

Nie należy wykonywać równoległego renderowania tego samego CustomColumn z wielu wątków bez dodatkowej synchronizacji.

Jeżeli renderowanie miałoby zostać przeniesione do wielu wątków, potrzebna byłaby zmiana architektury lub osobne instancje rendererów.

41. Kompletny przykład
Poniższy przykład łączy:

GridProperty,

GridViewAttribute,

GridAction,

CustomColumn,

GridFilter.

Model
public class Product : ICustomColumnData
{
    public int Id { get; set; }

    [GridView("Nazwa")]
    public string Name { get; set; }

    [GridView(
        "Cena",
        columnType: GridViewAttribute.EColumnType.Number,
        format: "N2")]
    public decimal Price { get; set; }

    [GridView(
        "Aktywny",
        columnType: GridViewAttribute.EColumnType.Boolean)]
    public bool IsActive { get; set; }

    [GridView(
        "Opis",
        columnType: GridViewAttribute.EColumnType.MemoEdit)]
    public string Description { get; set; }

    [GridView(
        "Utworzono",
        columnType: GridViewAttribute.EColumnType.Date)]
    public DateTime CreatedAt { get; set; }

    [GridView(
        "Edytuj",
        columnType: GridViewAttribute.EColumnType.Button,
        actionName: "Edit")]
    public string Edit { get; set; }
}

42. Widok CustomColumn
public class ProductView :
    UserControl,
    IGridView<Product>
{
    public void SetData(Product data)
    {
        nameLabel.Text = data.Name;
        priceLabel.Text =
            data.Price.ToString("N2");

        statusLabel.Text =
            data.IsActive
                ? "Aktywny"
                : "Nieaktywny";
    }
}

43. Inicjalizacja formularza
public partial class Form1 : Form
{
    private readonly GridProperty _gridProperty;

    private readonly List<Product> _products =
        new List<Product>();

    public Form1()
    {
        InitializeComponent();

        var renderHost =
            new ControlRenderHost();

        _gridProperty =
            new GridProperty(
                dataGridView1,
                renderHost);

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        _gridProperty.InitData<Product>();

        _gridProperty
            .GetAction<Product>("Edit")
            .Click += Edit_Click;

        dataGridView1.DataSource =
            _products;
    }

    private void Edit_Click(
        object sender,
        GridActionEventArgs<Product> e)
    {
        Product product = e.Item;

        MessageBox.Show(
            product.Name,
            "Edycja");
    }
}

Jeżeli aplikacja korzysta z GridFilter, może dodatkowo zainicjalizować:

private GridFilter _gridFilter;

oraz:

_gridFilter =
    new GridFilter(dataGridView1);

_gridFilter.Enable();

44. Zalecana kolejność inicjalizacji
W typowym formularzu kolejność powinna być następująca:

1. InitializeComponent()
        │
        ▼
2. Utworzenie RenderHost
        │
        ▼
3. Utworzenie GridProperty
        │
        ▼
4. InitData<T>()
        │
        ▼
5. Podpięcie GridAction
        │
        ▼
6. Ustawienie DataSource
        │
        ▼
7. Opcjonalnie GridFilter

Przykład:

public Form1()
{
    InitializeComponent();

    var renderHost =
        new ControlRenderHost();

    _gridProperty =
        new GridProperty(
            dataGridView1,
            renderHost);

    _gridProperty.InitData<Product>();

    _gridProperty
        .GetAction<Product>("Edit")
        .Click += Edit_Click;

    dataGridView1.DataSource =
        _products;

    _gridFilter =
        new GridFilter(dataGridView1);

    _gridFilter.Enable();
}

45. Minimalny przykład bez CustomColumn
Jeżeli aplikacja nie potrzebuje własnych widoków, wystarczy GridProperty.

public class Person
{
    [GridView("Imię")]
    public string Name { get; set; }

    [GridView(
        "Wiek",
        columnType: GridViewAttribute.EColumnType.Number)]
    public int Age { get; set; }

    [GridView(
        "Aktywny",
        columnType: GridViewAttribute.EColumnType.Boolean)]
    public bool IsActive { get; set; }
}

Formularz:

private readonly GridProperty _gridProperty;

public Form1()
{
    InitializeComponent();

    _gridProperty =
        new GridProperty(dataGridView1);

    _gridProperty.InitData<Person>();

    dataGridView1.DataSource =
        new List<Person>();
}

46. Minimalny przykład z akcją
Model:

public class Person
{
    [GridView("Imię")]
    public string Name { get; set; }

    [GridView(
        "Edytuj",
        columnType: GridViewAttribute.EColumnType.Button,
        actionName: "Edit")]
    public string Edit { get; set; }
}

Kod:

_gridProperty.InitData<Person>();

_gridProperty
    .GetAction<Person>("Edit")
    .Click += Edit_Click;

Handler:

private void Edit_Click(
    object sender,
    GridActionEventArgs<Person> e)
{
    MessageBox.Show(
        e.Item.Name);
}

47. Minimalny przykład CustomColumn
Model:

public class Person : ICustomColumnData
{
    public int Id { get; set; }

    public string Name { get; set; }
}

Widok:

public class PersonView :
    UserControl,
    IGridView<Person>
{
    public void SetData(Person data)
    {
        nameLabel.Text = data.Name;
    }
}

Architektura:

Person
  │
  ▼
CustomColumn<Person, PersonView>
  │
  ├── PersonView
  ├── AncherRenderHost
  └── BitmapCache
          │
          ▼
     DataGridView

48. Minimalny przykład filtrowania
Przykładowa definicja:

var filter =
    new FilterDefinition(
        "Name",
        FilterOperator.Contains,
        "Jan");

Możliwe jest również filtrowanie wartości liczbowych:

var filter =
    new FilterDefinition(
        "Price",
        FilterOperator.GreaterThan,
        100m);

lub wartości null:

var filter =
    new FilterDefinition(
        "Description",
        FilterOperator.IsNull);

49. Jak elementy biblioteki współpracują ze sobą?
Cała biblioteka może być przedstawiona jako trzy niezależne, ale współpracujące warstwy.

Warstwa konfiguracji
GridViewAttribute
       │
       ▼
GridProperty
       │
       ▼
DataGridView

Odpowiada za to jak wygląda tabela.

Warstwa danych
GridFilter
    │
    ▼
FilterDefinition
    │
    ▼
FilterExpressionBuilder
    │
    ▼
IDataSourceAdapter
    │
    ├── IEnumerable
    └── IQueryable

Odpowiada za to jak dane są filtrowane.

Warstwa prezentacji złożonej
TData
  │
  ▼
CustomColumn
  │
  ▼
TView
  │
  ▼
AncherRenderHost
  │
  ▼
BitmapCache
  │
  ▼
DataGridView

Odpowiada za to jak złożona zawartość jest prezentowana w komórce.

50. Pełny przepływ danych
Przy zastosowaniu wszystkich elementów:

                         MODEL
                           │
                           ▼
                 ┌──────────────────┐
                 │  GridProperty    │
                 │ GridViewAttribute│
                 └────────┬─────────┘
                          │
                          ▼
                    DataGridView
                          │
             ┌────────────┼────────────┐
             │            │            │
             ▼            ▼            ▼
          standard      Button      CustomColumn
          columns          │            │
             │             ▼            ▼
             │        GridAction      TView
             │                          │
             │                          ▼
             │                    RenderHost
             │                          │
             │                          ▼
             │                     BitmapCache
             │                          │
             └────────────┬─────────────┘
                          │
                          ▼
                     GridFilter
                          │
                          ▼
                 FilterDefinition
                          │
                          ▼
               FilterExpressionBuilder
                          │
                          ▼
                 IDataSourceAdapter
                          │
                   ┌──────┴──────┐
                   ▼             ▼
              IEnumerable   IQueryable

51. Odpowiedzialność biblioteki
Biblioteka celowo rozdziela trzy problemy:

GridProperty
    → konfiguracja UI

GridFilter
    → filtrowanie danych

CustomColumn
    → renderowanie złożonego UI

Dzięki temu aplikacja może używać:

GridProperty

bez:

GridFilter

albo:

GridProperty + GridFilter

albo pełnego zestawu:

GridProperty
+ GridFilter
+ CustomColumn

Elementy nie muszą być używane wszystkie jednocześnie.

52. Najważniejsze zasady CustomColumn
TData musi implementować ICustomColumnData.

Id jest podstawowym identyfikatorem cache.

TView musi być Control.

TView musi implementować IGridView<TData>.

Jedna instancja TView jest współdzielona przez CustomColumn.

TView powinien być traktowany jako renderer.

SetData() jest wykonywane przy CACHE MISS.

Renderowanie jest wykonywane przy CACHE MISS.

BitmapCache przechowuje byte[], a nie Bitmap.

Bitmapę zwróconą przez cache należy Dispose().

Cache wykorzystuje LRU.

Cache posiada limit pamięci.

Zmiana danych przy tym samym Id wymaga unieważnienia cache albo wersjonowania.

Paint() nie powinien wykonywać operacji biznesowych ani długotrwałego I/O.

Współdzielony TView zakłada sekwencyjne używanie renderera.

53. Najważniejsze zasady GridFilter
Źródło danych może być IEnumerable lub IQueryable.

FilterDefinition opisuje pojedynczy warunek.

FilterOperator określa sposób porównania.

Wiele filtrów jest łączonych przez AND.

Contains, StartsWith i EndsWith dotyczą właściwości string.

IsNull i IsNotNull służą do obsługi wartości pustych.

Dla IEnumerable wykorzystywane jest LINQ to Objects.

Dla IQueryable budowane jest Expression<Func<T, bool>>.

BindingSourceResolver pozwala rozwiązać zagnieżdżone BindingSource.

GridFilter jest opcjonalny.

54. Najważniejsze zasady GridProperty
Konfiguracja kolumn znajduje się przy modelu.

GridViewAttribute określa sposób prezentacji właściwości.

InitData<T>() inicjalizuje konfigurację DataGridView.

GetAction<T>() pobiera akcję dla kolumny Button.

GridAction<T>.Click obsługuje kliknięcie.

GridActionEventArgs<T>.Item zawiera obiekt modelu.

CustomColumn wymaga hosta renderowania.

GridFilter może działać niezależnie od GridProperty, ale typowy scenariusz łączy oba komponenty.

55. Najważniejszy model architektury
Całą bibliotekę można sprowadzić do następującego modelu:

                 ┌───────────────────────┐
                 │        MODEL          │
                 └───────────┬───────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
       GridProperty      GridFilter     CustomColumn
              │              │              │
              ▼              ▼              ▼
        konfiguracja      filtrowanie      renderowanie
              │              │              │
              └──────────────┼──────────────┘
                             │
                             ▼
                       DataGridView

W przypadku CustomColumn:

TData
  │
  ▼
Cache Key
  │
  ▼
BitmapCache
  │
  ├────────────── HIT ──────────────┐
  │                                 │
  └──────────── MISS                │
                  │                 │
                  ▼                 │
              TView.SetData()       │
                  │                 │
                  ▼                 │
          AncherRenderHost          │
                  │                 │
                  ▼                 │
              Bitmap                │
                  │                 │
                  ▼                 │
            BitmapCache             │
                  │                 │
                  └─────────────────┘
                            │
                            ▼
                       DrawImage()
                            │
                            ▼
                         Dispose

56. Podsumowanie
MC.Data.DataGrid jest biblioteką składającą się z trzech głównych obszarów:

GridProperty
Służy do konfiguracji DataGridView i definiowania jego kolumn poprzez model.

Model → GridViewAttribute → GridProperty → DataGridView

GridFilter
Służy do filtrowania danych znajdujących się w IEnumerable lub IQueryable.

FilterDefinition
      ↓
FilterExpressionBuilder
      ↓
IDataSourceAdapter
      ↓
Filtered DataSource

CustomColumn
Służy do renderowania złożonych widoków WinForms w komórkach.

TData
  ↓
TView
  ↓
AncherRenderHost
  ↓
Bitmap
  ↓
BitmapCache
  ↓
DataGridView

Najważniejszym założeniem CustomColumn jest:

TView = renderer
BitmapCache = pamięć wyników
CustomCell = integracja z DataGridView
AncherRenderHost = mechanizm renderowania

Dzięki temu nawet duża liczba wierszy nie wymaga tworzenia osobnej kontrolki WinForms dla każdej komórki.

W typowym zastosowaniu aplikacja może ograniczyć się do:

_gridProperty =
    new GridProperty(
        dataGridView1,
        renderHost);

_gridProperty.InitData<Product>();

_gridProperty
    .GetAction<Product>("Edit")
    .Click += Edit_Click;

_gridFilter =
    new GridFilter(dataGridView1);

_gridFilter.Enable();

dataGridView1.DataSource =
    products;

Natomiast szczegóły wyglądu tabeli, akcji, filtrowania oraz własnych komórek pozostają odpowiednio w:

GridViewAttribute
GridAction<T>
GridFilter
CustomColumn<TData, TView>

To rozdzielenie pozwala używać poszczególnych funkcjonalności niezależnie oraz łączyć je w jednym DataGridView.

To jest wersja dokumentacji nastawiona na użytkownika biblioteki, ale jednocześnie zachowuje opis architektury CustomColumn, BitmapCache, AncherRenderHost i adapterów IEnumerable/IQueryable, ponieważ są one istotne do prawidłowego używania i projektowania rozszerzeń biblioteki.




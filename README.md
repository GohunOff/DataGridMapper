# MC.Data.DataGrid
The MC.Data.DataGrid library extends the standard WinForms DataGridView with an extensive, yet easy-to-use mechanism for column configuration, data filtering, and rendering custom WinForms views directly inside cells.
The library has been designed so that typical usage requires only a small amount of code, while still allowing further extension and customization of DataGridView behavior.

#### Main Features
- declarative column configuration using GridViewAttribute,
- automatic column creation based on the model,
- support for text, numeric, date, time, bool, Button, Tooltip, MemoEdit, and CustomColumn columns,
- support for actions for Button columns,
- data filtering directly from the DataGridView,
- support for IEnumerable and IQueryable,
- support for nested BindingSource,
- filtering multiple properties simultaneously,
- support for null values,
- text filtering using Contains, StartsWith, and EndsWith,
- filtering numeric, date, and time values,
- filtering bool and enum values using convenient selection lists,
- rendering custom WinForms controls inside cells,
- one shared TView instance,
- rendering to Bitmap,
- in-memory LRU cache,
- cache memory limit,
- cache storing encoded byte[] instead of Bitmap objects,
- cache keys dependent on identifier, size, and rendering parameters,
- custom KeySelector support,
- CustomColumn integration with GridProperty metadata system,
- no need to create a control for every row.

#### Architecture
```
                         MC.Data.DataGrid
                                │
              ┌─────────────────┼─────────────────┐
              │                 │                 │
              ▼                 ▼                 ▼
        GridProperty        GridFilter       CustomColumn
              │                 │                 │
              ▼                 ▼                 ▼
        configuration       filtering         rendering
        DataGridView            data             custom UI
              │                 │                 │
              └─────────────────┼─────────────────┘
                                │
                                ▼
                         DataGridView
```
Each element can be used independently.

It is therefore possible to use:
```
GridProperty
```
or:
```
GridProperty + GridFilter
```
or:
```
GridProperty + CustomColumn
```
or the complete set:
```
GridProperty
+ GridFilter
+ CustomColumn
```
### 1. GridProperty
GridProperty is responsible for declarative DataGridView configuration based on the data model.

Instead of manually creating and configuring every column:
```
dataGridView1.Columns.Add(...);
dataGridView1.Columns[0].HeaderText = "...";
dataGridView1.Columns[0].Width = ...;
```
the configuration can be placed directly next to the model property:
```
[GridView("Name")]
public string Name { get; set; }
```
Then:
```
_gridProperty.InitData<Product>();
```
automatically prepares the columns.

#### Advantages of GridProperty
- column configuration is located next to the model,
- less code in the form,
- consistent configuration,
- automatic property recognition,
- ability to hide properties,
- formatting support,
- editing support,
- button support,
- ability to use CustomColumn.

### 2. GridViewAttribute
GridViewAttribute defines how a model property should be displayed.

Example:

public class Product
{
    [GridView("Name")]
    public string Name { get; set; }

    [GridView(
        "Price",
        columnType: GridViewAttribute.EColumnType.Number)]
    public decimal Price { get; set; }
}

Constructor
```
GridViewAttribute(
    string name,
    bool ignore = false,
    bool visibility = true,
    bool allowEdit = false,
    string columnName = "",
    string format = "",
    EColumnType columnType = EColumnType.None,
    string actionName = "")
```
#### Parameters
- Parameter Meaning
- name Header text
- ignore Completely omit the property
- visibility Column visibility
- allowEdit Whether editing is allowed
- columnName DataGridView column name
- format Value display format
- columnType Column type
- actionName Action name for Button

### 3. Column Types
The library supports, among others:
- None
- Text
- Number
- Date
- Time
- DateTime
- Button
- Tooltip
- MemoEdit
- Boolean
- CustomColumn
- None

#### Standard column.
```
[GridView("Name")]
public string Name { get; set; }
```
Text
```
[GridView(
    "Name",
    columnType: GridViewAttribute.EColumnType.Text)]
public string Name { get; set; }
```
Number

The following numeric types are supported:
- byte
- sbyte
- short
- ushort
- int
- uint
- long
- ulong
- float
- double
- decimal

Example:
```
[GridView(
    "Price",
    columnType: GridViewAttribute.EColumnType.Number)]
public decimal Price { get; set; }
```
The default format can be configured through the library configuration, for example:
```
N2
```
Custom format:
```
[GridView(
    "Price",
    format: "N0",
    columnType: GridViewAttribute.EColumnType.Number)]
public decimal Price { get; set; }
```
Boolean

Supported types:
```
bool
bool?
```
Example:
```
[GridView(
    "Active",
    columnType: GridViewAttribute.EColumnType.Boolean)]
public bool IsActive { get; set; }
```
Date
```
[GridView(
    "Date",
    columnType: GridViewAttribute.EColumnType.Date)]
public DateTime CreatedAt { get; set; }
```
Example format:
```
dd.MM.yyyy
```
Custom format:
```
[GridView(
    "Date",
    format: "yyyy-MM-dd",
    columnType: GridViewAttribute.EColumnType.Date)]
public DateTime CreatedAt { get; set; }
```
Time

Supports:
```
TimeSpan
TimeSpan?
DateTime
DateTime?
```
Example:
```
[GridView(
    "Time",
    columnType: GridViewAttribute.EColumnType.Time)]
public TimeSpan Duration { get; set; }
```
DateTime
```
[GridView(
    "Created",
    columnType: GridViewAttribute.EColumnType.DateTime)]
public DateTime CreatedAt { get; set; }
```
Example format:
```
dd.MM.yyyy HH:mm:ss
```
Tooltip
```
[GridView(
    "Information",
    columnType: GridViewAttribute.EColumnType.Tooltip)]
public string Information { get; set; }
```
MemoEdit

Intended for longer text:
```
[GridView(
    "Description",
    allowEdit: true,
    columnType: GridViewAttribute.EColumnType.MemoEdit)]
public string Description { get; set; }
```
Button
```
[GridView(
    "Edit",
    columnType: GridViewAttribute.EColumnType.Button,
    actionName: "Edit")]
public string Edit { get; set; }
```
The action can then be obtained:
```
_gridProperty
    .GetAction<Product>("Edit")
    .Click += Edit_Click;
```
### 4. Visibility and Ignore
A column can be hidden:
```
[GridView(
    "Technical Value",
    visibility: false)]
public string TechnicalValue { get; set; }
```
A column can also be completely omitted:
```
[GridView(
    "Ignored Value",
    ignore: true)]
public string IgnoredValue { get; set; }
```
Difference:
```
visibility: false
    ↓
column exists, but is not visible

ignore: true
    ↓
property is skipped during configuration
```
### 5. AllowEdit
By default, columns are intended for read-only use.

Editing can be enabled:
```
[GridView(
    "Name",
    allowEdit: true)]
public string Name { get; set; }
```
For longer text:
```
[GridView(
    "Description",
    allowEdit: true,
    columnType: GridViewAttribute.EColumnType.MemoEdit)]
public string Description { get; set; }
```
### 6. ColumnName
name specifies the text visible to the user.

columnName specifies the DataGridView column name.
```
[GridView(
    "User Name",
    columnName: "UserName")]
public string Name { get; set; }
```
### 7. GridAction
Button columns can have an action.

Model:
```
[GridView(
    "Edit",
    columnType: GridViewAttribute.EColumnType.Button,
    actionName: "Edit")]
public string Edit { get; set; }
```
Getting the action:
```
_gridProperty
    .GetAction<Product>("Edit")
    .Click += Edit_Click;

GridAction<T>
```
The action contains:
```
string Key
```
and:
```
event EventHandler<GridActionEventArgs<T>> Click

GridActionEventArgs<T>
```
The event argument contains:
- Item
- Grid
- RowIndex
- ColumnIndex

The most important property is:
```
e.Item
```
which contains the model object associated with the clicked row.

Example:
```
private void Edit_Click(
    object sender,
    GridActionEventArgs<Product> e)
{
    Product product = e.Item;

    MessageBox.Show(product.Name);
}
```
## 8. GridFilter
GridFilter adds a filtering panel to the DataGridView.

Example:
```
_gridFilter =
    new GridFilter(dataGridView1);

_gridFilter.Enable();
```
Filters are created based on GridPropertyMetadata.

Architecture:
```
DataGridViewColumn
        │
        ▼
GridPropertyMetadata
        │
        ▼
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
```
### 9. Supported Operators
- Equals
- NotEquals
- Contains
- StartsWith
- EndsWith
- GreaterThan
- GreaterThanOrEqual
- LessThan
- LessThanOrEqual
- IsNull
- IsNotNull

Examples:
```
Age == 18
Age != 18
Name.Contains("Jan")
Name.StartsWith("Jan")
Name.EndsWith("ski")
Age > 18
Age >= 18
Age < 18
Age <= 18
Name == null
Name != null
```

### 10. GridFilter Interface
For typical text, numeric, date, and time properties, the following layout is available:
```
┌───────────────┬──────────────────┐
│ operator      │ value            │
└───────────────┴──────────────────┘
```
For bool:
```
┌──────────────────────────────┐
│ All / Yes / No               │
└──────────────────────────────┘
```
For enum:
```
┌──────────────────────────────┐
│ All / enum value             │
└──────────────────────────────┘
```
This means the user does not have to enter true / false or the textual representation of an enum.

### 11. Bool Filtering
For:
```
public bool IsActive { get; set; }
```
the panel contains:
- All
- Yes
- No

Selecting Yes creates:
```
FilterOperator.Equals
```
with the value:
```
true
```
Selecting No creates:
```
FilterOperator.Equals
```
with the value:
```
false
```
### 12. Enum Filtering
For:
```
public ProductStatus Status { get; set; }
```
the filter automatically creates:
- All
- New
- Processing
- Completed
- Cancelled

Selecting a value results in filtering:
```
Status == selectedValue
```
### 13. DateTime and TimeSpan
The filter uses strict input formats.

DateTime
```
dd.MM.yyyy
```
Example:
```
25.09.2026
```
TimeSpan
```
hh:mm:ss
```
Example:
```
08:30:00
```
The column display format and the format entered into the filter are independent mechanisms.

### 14. FilterDefinition
FilterDefinition describes a single condition:
```
var filter =
    new FilterDefinition(
        "Name",
        FilterOperator.Contains,
        "Jan");
```
Available properties:
- PropertyName
- Operator
- Value

Numeric example:
```
var filter =
    new FilterDefinition(
        "Price",
        FilterOperator.GreaterThan,
        100m);
```
Null filter:
```
var filter =
    new FilterDefinition(
        "Description",
        FilterOperator.IsNull,
        null);
```
### 15. Multiple Filters
Active filters are combined using logical AND.

Example:
```
Name contains "Jan"
AND
Age >= 18
AND
IsActive == true
```
corresponds to:
```
Name.Contains("Jan")
    &&
Age >= 18
    &&
IsActive == true
```
### 16. IEnumerable
The library supports sources located in memory.

Example:
```
List<Product> products;
```
Flow:
```
IEnumerable<Product>
        │
        ▼
EnumerableDataSourceAdapter<Product>
        │
        ▼
LINQ to Objects
        │
        ▼
Filtered result
```
This allows filtering of, among others:
```
List<T>
IEnumerable<T>
```
in-memory collections.

### 17. IQueryable
The library also supports:
```
IQueryable<Product>
```
Flow:
```
IQueryable<Product>
        │
        ▼
QueryableDataSourceAdapter<Product>
        │
        ▼
Expression<Func<Product,bool>>
        │
        ▼
Queryable.Where(...)
```
For IQueryable, the filter remains an expression tree.
This allows the LINQ provider to decide how the expression should be executed.
For example, a database provider can translate the expression into an SQL query.

### 18. BindingSource
GridFilter can work with a source:
```
BindingSource
```
It can also resolve nested BindingSource:
```
BindingSource
      │
      ▼
BindingSource
      │
      ▼
List<Product>
```
Mechanism:
```
DataGridView
      │
      ▼
GridFilter
      │
      ▼
BindingSource
      │
      ▼
BindingSourceResolver
      │
      ▼
actual source
```
If DataGridView.DataSource is not a BindingSource, GridFilter can create its own BindingSource.
After disabling the filter, the original data source is restored.

### 19. Enable / Disable
Enable:
```
_gridFilter.Enable();
```
Disable:
```
_gridFilter.Disable();
```
Enable():
- checks the DataSource,
- prepares the BindingSource,
- creates the FilterContext,
- creates the filter panel,
- creates the controls,
- attaches events,
- synchronizes control positions with columns.

Disable():
- detaches events,
- removes the panel,
- releases the controls,
- restores the previous DataSource.

### 20. SetFilter
Filters can also be set programmatically:
```
_gridFilter.SetFilter(
    "Price",
    FilterOperator.GreaterThan,
    100m);
```
It is therefore possible to combine:
```
user filters
+
programmatically configured filters
```
The mechanism stores active filters by property name.

## 21. CustomColumn
CustomColumn<TData,TView> allows a custom WinForms view to be displayed inside an individual cell.
It is not simply text or a standard DataGridViewCell.

Architecture:
```
TData
  │
  ▼
CustomColumn<TData,TView>
  │
  ├── TView
  ├── ControlRenderHost
  ├── BitmapCache
  ├── KeySelector
  └── GridPropertyMetadata
```
The most important feature is the ability to use an existing UserControl as the cell renderer.

### 22. ICustomColumnData
The model used by CustomColumn must implement:
```
public interface ICustomColumnData
{
    int Id { get; set; }
}
```
Example:
```
public class Person : ICustomColumnData
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Address { get; set; }

    public bool IsActive { get; set; }
}
```
Id is the primary element identifying the data for the cache.

### 23. IGridView<TData>
The view must:
- inherit from Control,
- implement IGridView<TData>.

Example:
```
public class PersonView :
    UserControl,
    IGridView<Person>
{
    public void SetData(Person data)
    {
        nameLabel.Text = data.Name;
        addressLabel.Text = data.Address;

        statusLabel.Text =
            data.IsActive
                ? "Active"
                : "Inactive";
    }
}
```
SetData() prepares the shared renderer for displaying a specific object.

### 24. One TView Instance
CustomColumn has one shared instance:
```
View = new TView();
```
A control is not created for every cell.

For:
```
1000 rows
```
this does not result in:
```
1000 × TView
```
but:
```
1 × TView
1 × BitmapCache
```
Flow:
```
CustomColumn
      │
      └── TView
```
The instance is shared during subsequent rendering operations.
This significantly reduces the number of WinForms controls and the associated memory overhead.

### 25. TView as a Renderer
TView should be treated as a temporary renderer.

Example:
```
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

then:

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
```
The state of TView does not represent a specific cell.

The result is a bitmap stored in the cache.

### 26. ControlRenderHost
ControlRenderHost is responsible for rendering a control to a Bitmap.
It is responsible, among other things, for:
- hosting TView,
- setting the size,
- performing layout,
- preparing the control,
- DrawToBitmap(),
- returning the generated bitmap.

Depending on the library version, the rendering infrastructure may also be referred to as AncherRenderHost.

Architecture:
```
CustomColumn
      │
      ▼
TView
      │
      ▼
ControlRenderHost
      │
      ├── attach
      ├── Size
      ├── Layout
      └── DrawToBitmap()
              │
              ▼
           Bitmap
```           
### 27. CustomColumn Initialization
When using CustomColumn, the rendering host must be prepared.

Example:
```
private readonly ControlRenderHost _renderHost;

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
```
GridProperty can then create the appropriate CustomColumn.

### 28. CustomColumn Metadata
CustomColumn implements:
```
IGridPropertyMetadataProvider
```
and has:
```
public GridPropertyMetadata PropertyMetadata
{
    get;
    private set;
}
```
The metadata is created based on the TData property.

The constructor searches for:
```
typeof(TData).GetProperty(propertyName)
```
and then retrieves:
```
GridViewAttribute
```
and creates:
```
new GridPropertyMetadata(
    property,
    attribute);
```
DataPropertyName is set to:
```
PropertyMetadata.PropertyName
```
This keeps CustomColumn connected to the model property in the same way as the other column types.

### 29. KeySelector
CustomColumn has:
```
Func<TData, object> KeySelector
```
By default:
```
KeySelector = value => value;
```
The mechanism allows customization of how data is identified when building the cache key.
This is an extension point for more advanced scenarios.

### 30. CustomCell.Paint()
CustomCell is responsible for integrating the renderer with DataGridView.

Flow:
```
Paint()
   │
   ├── get CustomColumn
   │
   ├── get TData
   │
   ├── create BitmapCacheKey
   │
   ├── Cache.TryGet()
   │
   ├── HIT
   │     └── DrawImage()
   │
   └── MISS
         │
         ├── View.SetData(data)
         ├── RenderHost.Render(View)
         ├── Cache.Set(bitmap)
         ├── Cache.TryGet()
         └── DrawImage()
```
### 31. Cache HIT
If the bitmap is already in the cache:
```
Cache
  │
  ▼
Bitmap
  │
  ▼
DrawImage()
```
the following operations are not performed again:
```
View.SetData()
RenderHost.Render()
```
This is the most important optimization of CustomColumn.

### 32. Cache MISS
If the image is not in the cache:
```
Cache.TryGet()
      │
      ▼
     MISS
      │
      ▼
View.SetData(data)
      │
      ▼
RenderHost.Render()
      │
      ▼
Bitmap
      │
      ▼
Cache.Set()
      │
      ▼
DrawImage()
```
Rendering is primarily performed on the first use of a specific image version.

### 33. BitmapCache
BitmapCache stores the rendering result.

The cache should not store objects directly:
```
Bitmap
```
but rather:
```
byte[]
```
Storage:
```
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
```
Reading:
```
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
```
### 34. Why Does the Cache Store byte[]?
Bitmap is a GDI+ object and owns native resources.

Storing encoded data:
```
byte[]
```
allows the lifetime of the cache entry to be separated from the temporary Bitmap instance.

The bitmap retrieved from the cache should be disposed by the code that uses it.

Example:
```
if (column.Cache.TryGet(key, out Bitmap bitmap))
{
    using (bitmap)
    {
        graphics.DrawImage(
            bitmap,
            cellBounds);
    }
}
```
### 35. LRU Cache
BitmapCache uses:
```
LRU
Least Recently Used
```
The most frequently used elements remain at the beginning of the structure.
```
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
```
When the memory limit is exceeded, the least recently used entries are removed.

### 36. Memory Limit
The cache has a limit defined by:
```
MaxMemoryMB
```
Example:
```
new BitmapCache(20);
```
means approximately:
```
20 MB
```
allocated for encoded image data.

If a single image is larger than the entire available limit, it may be skipped.

### 37. BitmapCacheKey
The cache key should identify a specific version of an image.

For example:
```
new BitmapCacheKey(
    data.Id,
    cellBounds.Size,
    ImageFormat.Png,
    90L);
```
The image then depends, among other things, on:
- Id
- Size
- ImageFormat
- Quality

The same object:
```
Id = 10
```
can therefore have different entries:
```
10 + 200x80
10 + 300x100
```
### 38. Data Changes and Cache
Id alone does not guarantee that the image is still current.

If:
```
Id = 10
```
remains the same, but the visual data changes:
```
Name
Address
Status
...
```
the old bitmap may still be present in the cache.

One of the following methods should then be used:
```
Immutable data
```
After the object is created, visual data is not modified.
```
Cache invalidation
```
After changing the data, the appropriate entry is removed.
```
Versioning
```
The key can contain:
```
Id + Version + Size + Format + Quality
```
### 39. Paint() Performance
DataGridView.Paint() can be called many times.

The following should not be performed inside Paint():
- database queries,
- long-running I/O,
- business operations,
- data loading,
- expensive operations.

Desired flow:
```
CACHE HIT
    │
    ▼
DrawImage()
```
should be as fast as possible.

### 40. Bitmap Lifecycle
Rendering:
```
TView
  │
  ▼
RenderHost
  │
  ▼
Bitmap
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
```
The bitmap used to store the data can then be released.

Display:
```
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
```
### 41. CustomColumn Concurrency
TView is a WinForms control and one instance is shared.

Therefore, the renderer should not be used concurrently from multiple threads.

Typical flow:
```
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
```
is consistent with the architectural assumptions.

If rendering were to be performed concurrently, the architecture would need to be changed or separate renderer instances would need to be used.

### 42. Advantages of CustomColumn
CustomColumn allows existing WinForms controls to be used as cell content.

It is therefore possible to create cells containing:
```
┌──────────────────────────────┐
│ John Smith                   │
│ 10 Piotrkowska St.           │
│ Pabianice                    │
│ ● Active                     │
└──────────────────────────────┘
```
without creating a separate UserControl for every row.

The most important advantages:
- reuse of existing UserControls,
- one TView instance,
- rendering to bitmap,
- result caching,
- LRU,
- memory limit,
- custom key support,
- integration with GridProperty,
- ability to display very complex UI elements.

### 43. Complete Data Model
Example:
```
public class Product : ICustomColumnData
{
    public int Id { get; set; }

    [GridView("Name")]
    public string Name { get; set; }

    [GridView(
        "Price",
        columnType: GridViewAttribute.EColumnType.Number,
        format: "N2")]
    public decimal Price { get; set; }

    [GridView(
        "Active",
        columnType: GridViewAttribute.EColumnType.Boolean)]
    public bool IsActive { get; set; }

    [GridView(
        "Description",
        columnType: GridViewAttribute.EColumnType.MemoEdit)]
    public string Description { get; set; }

    [GridView(
        "Created",
        columnType: GridViewAttribute.EColumnType.Date)]
    public DateTime CreatedAt { get; set; }

    [GridView(
        "Edit",
        columnType: GridViewAttribute.EColumnType.Button,
        actionName: "Edit")]
    public string Edit { get; set; }
}
```
### 44. CustomColumn View
```
public class ProductView :
    UserControl,
    IGridView<Product>
{
    public void SetData(Product data)
    {
        nameLabel.Text =
            data.Name;

        priceLabel.Text =
            data.Price.ToString("N2");

        statusLabel.Text =
            data.IsActive
                ? "Active"
                : "Inactive";
    }
}
```
### 45. GridProperty Initialization
When CustomColumn is used, a ControlRenderHost must be prepared:
```
private readonly GridProperty _gridProperty;
private readonly GridFilter _gridFilter;

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

46. Action Handling
private void Edit_Click(
    object sender,
    GridActionEventArgs<Product> e)
{
    Product product = e.Item;

    MessageBox.Show(
        product.Name,
        "Edit");
}
```
### 47. Recommended Initialization Order
A typical form should perform:
```
1. InitializeComponent()
        │
        ▼
2. ControlRenderHost
        │
        ▼
3. GridProperty
        │
        ▼
4. InitData<T>()
        │
        ▼
5. GridAction
        │
        ▼
6. DataSource
        │
        ▼
7. GridFilter
```
Example:
```
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
```
### 48. Minimal Example Without CustomColumn
If the application does not require complex views:
```
public class Person
{
    [GridView("Name")]
    public string Name { get; set; }

    [GridView(
        "Age",
        columnType: GridViewAttribute.EColumnType.Number)]
    public int Age { get; set; }

    [GridView(
        "Active",
        columnType: GridViewAttribute.EColumnType.Boolean)]
    public bool IsActive { get; set; }
}
```
Form:
```
public Form1()
{
    InitializeComponent();

    _gridProperty =
        new GridProperty(dataGridView1);

    _gridProperty.InitData<Person>();

    dataGridView1.DataSource =
        new List<Person>();
}
```
### 49. Minimal Action Example
Model:
```
public class Person
{
    [GridView("Name")]
    public string Name { get; set; }

    [GridView(
        "Edit",
        columnType: GridViewAttribute.EColumnType.Button,
        actionName: "Edit")]
    public string Edit { get; set; }
}
```
Code:
```
_gridProperty.InitData<Person>();

_gridProperty
    .GetAction<Person>("Edit")
    .Click += Edit_Click;
```    
Handler:
```
private void Edit_Click(
    object sender,
    GridActionEventArgs<Person> e)
{
    MessageBox.Show(
        e.Item.Name);
}
```
### 50. Minimal CustomColumn Example
Model:
```
public class Person : ICustomColumnData
{
    public int Id { get; set; }

    public string Name { get; set; }
}
```
View:
```
public class PersonView :
    UserControl,
    IGridView<Person>
{
    public void SetData(Person data)
    {
        nameLabel.Text =
            data.Name;
    }
}
```
Architecture:
```
Person
  │
  ▼
CustomColumn<Person, PersonView>
  │
  ├── PersonView
  ├── ControlRenderHost
  └── BitmapCache
```
### 51. Minimal Filtering Example
Programmatic filtering:
```
_gridFilter.SetFilter(
    "Name",
    FilterOperator.Contains,
    "Jan");
```
Numeric filtering:
```
_gridFilter.SetFilter(
    "Price",
    FilterOperator.GreaterThan,
    100m);
```
Null filtering:
```
_gridFilter.SetFilter(
    "Description",
    FilterOperator.IsNull,
    null);
```
52. Complete Data Flow
The entire library can be represented as follows:
```
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
             ┌────────────┼─────────────┐
             │            │             │
             ▼            ▼             ▼
          standard      Button      CustomColumn
          columns         │             │
             │            ▼             ├── TView
             │       GridAction          ├── RenderHost
             │                           ├── BitmapCache
             │                           └── PropertyMetadata
             │
             └────────────┬─────────────┘
                          │
                          ▼
                     GridFilter
                          │
                          ▼
                    BindingSource
                          │
                          ▼
                 BindingSourceResolver
                          │
                          ▼
                IDataSourceAdapter
                    │             │
                    ▼             ▼
               IEnumerable    IQueryable
```
## 53. Component Responsibilities
GridProperty
Responsible for:
- DataGridView configuration,
- column creation,
- using GridViewAttribute,
- formatting,
- editing,
- buttons,
- actions,
- CustomColumn integration.

GridFilter
Responsible for:
- filtering interface,
- FilterDefinition,
- FilterOperator,
- filtering multiple properties,
- IEnumerable,
- IQueryable,
- BindingSource,
- updating the data source.

CustomColumn
Responsible for:
- integration with DataGridViewColumn,
- TView,
- PropertyMetadata,
- RenderHost,
- BitmapCache,
- KeySelector.

CustomCell
Responsible for:
- Paint(),
- retrieving data,
- building the cache key,
- Cache.TryGet(),
- handling HIT/MISS,
- drawing the bitmap.

TView
Responsible for:
- view appearance,
- SetData(),
- preparing renderer state.

ControlRenderHost
Responsible for:
- hosting the control,
- layout,
- size,
- DrawToBitmap().

BitmapCache
Responsible for:
- bitmap caching,
- encoding,
- decoding,
- LRU,
- memory limit.

## 54. Most Important GridProperty Rules
Column configuration is located next to the model.
GridViewAttribute defines how a property is displayed.
InitData<T>() initializes the configuration.
GetAction<T>() retrieves a button action.
GridActionEventArgs<T>.Item contains the model.
CustomColumn can participate in the metadata system.
CustomColumn requires a rendering host.

## 55. Most Important GridFilter Rules
The source can be IEnumerable.
The source can be IQueryable.
BindingSource can be used.
Nested BindingSource is supported.
FilterDefinition describes a single condition.
FilterOperator defines the operator.
Multiple filters are combined using AND.
bool has a dedicated ComboBox.
enum has a dedicated ComboBox.
DateTime uses the dd.MM.yyyy input format.
TimeSpan uses the hh:mm:ss input format.
IsNull and IsNotNull support empty values.
SetFilter() allows programmatic filter control.
Disable() restores the previous state of the data source.

## 56. Most Important CustomColumn Rules
TData must implement ICustomColumnData.
TView must be a Control.
TView must implement IGridView<TData>.
TView is shared.
TView should be treated as a renderer.
SetData() prepares the renderer.
Rendering is primarily performed on CACHE MISS.
CACHE HIT should result in a fast DrawImage().
BitmapCache stores byte[].
The bitmap returned from the cache should be disposed.
The cache uses LRU.
The cache has a memory limit.
BitmapCacheKey can include size and image parameters.
KeySelector allows customization of data identification.
Changing data with the same key requires cache invalidation or versioning.
Paint() should not perform business operations or long-running I/O.
The shared TView assumes sequential use of the renderer.
CustomColumn implements IGridPropertyMetadataProvider.

## 57. Advantages of the Library
Less code

Configuration can be located directly next to the model:
```
[GridView("Name")]
public string Name { get; set; }
```
instead of manually configuring every column.

Declarative approach

The model describes how the data should be displayed.
```
Model
  │
  ├── column name
  ├── column type
  ├── format
  ├── visibility
  ├── editing
  └── action
```
Reusability

GridProperty, GridFilter, and CustomColumn can be used independently.

Support for Different Data Sources

The library can work with:
```
List<T>
IEnumerable<T>
IQueryable<T>
BindingSource
```
Separation of Responsibilities
```
GridProperty
    → configuration

GridFilter
    → filtering

CustomColumn
    → rendering
```
Each component has a clearly defined responsibility.

Efficient CustomColumn

Instead of:
```
1000 rows
↓
1000 UserControls
```
the following is used:
```
1000 rows
↓
1 shared TView
↓
BitmapCache
```
LRU Cache

The most frequently used bitmaps remain in memory, while unused ones are automatically removed when the memory limit is exceeded.

Memory Control

The cache has a memory limit, allowing its size to be controlled by the application.

No Need to Store Bitmaps

The cache stores byte[] rather than GDI+ Bitmap objects.

Extensibility

Extension points include, among others:
```
IGridView<T>
ICustomColumnData
IGridPropertyMetadataProvider
IDataSourceAdapter
KeySelector
GridViewAttribute
FilterOperator
FilterDefinition
```
## 58. Recommended Practices
For GridProperty

Keep presentation configuration next to the model:
```
[GridView(...)]
```
Instead of manually configuring columns in multiple forms.

For GridFilter

Set the DataSource before calling:
```
_gridFilter.Enable();
```
For CustomColumn

Treat TView as a renderer rather than the state of a specific cell.

For the Cache

If data is mutable, plan a mechanism for:
```
invalidate
```
or:
```
versioning
```
For Paint()

Avoid:
```
database query
network I/O
file I/O
business logic
```
in the Paint() execution path.

## 59. Full Architecture
```
                         ┌───────────────────────┐
                         │        MODEL          │
                         └───────────┬───────────┘
                                     │
                  ┌──────────────────┼──────────────────┐
                  │                  │                  │
                  ▼                  ▼                  ▼
           GridViewAttribute   ICustomColumnData   PropertyInfo
                  │                  │                  │
                  ▼                  │                  ▼
            GridProperty             │          GridPropertyMetadata
                  │                  │                  │
                  ▼                  │                  │
            DataGridView             │                  │
                  │                  │                  │
       ┌──────────┼───────────┐     │                  │
       │          │           │     │                  │
       ▼          ▼           ▼     ▼                  ▼
    Standard   Button    CustomColumn ──────────► PropertyMetadata
       │          │           │
       │          ▼           ├── TView
       │     GridAction       ├── RenderHost
       │                      ├── BitmapCache
       │                      └── KeySelector
       │
       └──────────────────────────────┐
                                      │
                                      ▼
                                 GridFilter
                                      │
                                      ▼
                                 BindingSource
                                      │
                                      ▼
                             BindingSourceResolver
                                      │
                                      ▼
                              IDataSourceAdapter
                                │             │
                                ▼             ▼
                           IEnumerable    IQueryable
```
## 60. Summary
MC.Data.DataGrid combines three independent mechanisms:
```
GridProperty
    ↓
DataGridView configuration

GridFilter
    ↓
data filtering

CustomColumn
    ↓
complex UI rendering
```
The most important element of the CustomColumn architecture is the separation of:
```
TView
    = renderer

ControlRenderHost
    = rendering mechanism

BitmapCache
    = result storage

CustomCell
    = DataGridView integration
```
This allows a complex WinForms view to be used as cell content without creating a separate control for every row.

The entire library can be used both in simple forms:
```
_gridProperty =
    new GridProperty(dataGridView1);

_gridProperty.InitData<Person>();

and in more advanced solutions:

_gridProperty =
    new GridProperty(
        dataGridView1,
        renderHost);

_gridProperty.InitData<Product>();

_gridProperty
    .GetAction<Product>("Edit")
    .Click += Edit_Click;

dataGridView1.DataSource =
    products;

_gridFilter =
    new GridFilter(dataGridView1);

_gridFilter.Enable();
```
The architecture remains modular:
```
                MC.Data.DataGrid
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
  GridProperty     GridFilter    CustomColumn
        │              │              │
        ▼              ▼              ▼
 configuration      filtering      rendering
        │              │              │
        └──────────────┼──────────────┘
                       ▼
                 DataGridView
```
The main idea of the library is to combine declarative configuration, flexible filtering, and efficient rendering of complex views while maintaining the standard DataGridView workflow in WinForms.
## Requirements

- .NET Framework 4.8
- C# or another compatible .NET development environment

MC.Data.DataGrid does not require additional external dependencies.

---

## License

This project is licensed under the MIT License.

See [`LICENSE.txt`](LICENSE.txt) for the complete license text.

Copyright (c) 2026 gohunoff@gmail.com

**Author:** Przemysław Załuska  
**Email:** gohunoff@gmail.com

**GitHub:**  [MC.Data.DataGrid on GitHub]https://github.com/GohunOff/DataGridMapper

MC.Data.DataGrid is developed and maintained by the author.

If you find a problem, have a feature request, or would like to contribute, please open an issue in the GitHub repository.
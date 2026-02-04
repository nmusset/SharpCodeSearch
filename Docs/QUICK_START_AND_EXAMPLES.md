# SharpCodeSearch - Quick Start & Examples

## Getting Started

### Prerequisites
- .NET 8 SDK or later
- Node.js 16+ and npm
- VS Code 1.80 or later
- Git

### Development Environment Setup

**1. Clone and Navigate:**
```bash
git clone https://github.com/yourusername/SharpCodeSearch.git
cd SharpCodeSearch
```

**2. Install Extension Dependencies:**
```bash
cd src/extension
npm install
cd ../..
```

**3. Build Backend:**
```bash
dotnet build src/backend/SharpCodeSearch.csproj -c Release
```

**4. Launch Extension in Debug Mode:**
```bash
# From VS Code: Press F5 or use Debug menu
```

---

## Basic Pattern Examples

### Example 1: Find All ToString() Calls

**Pattern:**
```
$obj$.ToString()
```

**What it matches:**
```csharp
user.ToString()          // ✓ obj = user
config.ToString()        // ✓ obj = config
item.ToString()          // ✓ obj = item
x.ToString().Trim()      // ✗ doesn't match (has method call after)
```

**When to use:**
- Refactoring to use null-coalescing: `$obj$?.ToString() ?? ""`
- Finding potential null reference exceptions

---

### Example 2: Find Field Initialization with Specific Type

**Pattern:**
```
public $type:IEnumerable$ $field$ = $value$;
```

**What it matches:**
```csharp
public List<string> Names = new List<string>();        // ✓
public IEnumerable<int> Items = new int[] { 1, 2, 3 }; // ✓
public string[] Array = new string[10];                  // ✗ (not IEnumerable directly)
public IEnumerable<string> Values;                       // ✗ (no initialization)
```

**Advanced Constraints:**
```
public $type:[System\.Collections\.Generic\.IEnumerable.*]$ $field$ = $value$;
```

---

### Example 3: Find Anti-Pattern - If-Else Returning Boolean

**Pattern:**
```
if ($condition$)
{
    return true;
}
else
{
    return false;
}
```

**What it matches:**
```csharp
// ✓ Matches
if (user.IsActive)
{
    return true;
}
else
{
    return false;
}

// ✓ Also matches (variations)
if (x > 5)
{
    return true;
}
else
{
    return false;
}
```

**Refactoring Replacement:**
```
return $condition$;
```

---

### Example 4: Find Async Methods Without ConfigureAwait

**Pattern:**
```
await $expression$;
```

**Enhancement - Add Semantic Constraint:**
```
await $expression$;
// With constraint: Search only in methods not returning Task
```

**Replacement:**
```
await $expression$.ConfigureAwait(false);
```

---

### Example 5: Find Constructor Calls with New Expression

**Pattern:**
```
new $class:.*Service$($args$)
```

**What it matches:**
```csharp
new UserService(context)                    // ✓ class = UserService
new OrderProcessingService(db, logger)      // ✓ class = OrderProcessingService
new Utils()                                  // ✗ (doesn't end with Service)
new DatabaseService(config, logger)         // ✓ class = DatabaseService
```

**Use case:** Identifying service instantiation for dependency injection refactoring

---

### Example 6: Find Dictionary Initialization Patterns

**Pattern:**
```
Dictionary<$keyType$, $valueType$> $dict$ = new();
```

**What it matches:**
```csharp
Dictionary<string, int> counters = new();           // ✓
Dictionary<string, List<int>> mapping = new();      // ✓
var dict = new Dictionary<string, int>();          // ✗ (uses var)
Dictionary<int, string> lookup = new Dictionary<int, string>(); // ✗ (explicit type)
```

---

### Example 7: Find NULL Checks Before Method Calls

**Pattern:**
```
if ($obj$ != null)
{
    $obj$.$method$($args$);
}
```

**What it matches:**
```csharp
// ✓ Matches
if (user != null)
{
    user.Save(context);
}

// ✗ Different operators
if (user is not null)
{
    user.Save(context);
}
```

**Modern Replacement Pattern:**
```
$obj$?.$method$($args$);
```

---

### Example 8: Find Event Handler Registration

**Pattern:**
```
$event$ += $handler$;
```

**What it matches:**
```csharp
button.Click += OnClick;              // ✓
form.Submit += HandleSubmit;          // ✓
logger.ErrorOccurred += OnError;      // ✓
```

**With Type Constraint:**
```
$event:[.*Clicked$]$ += $handler$;
```

---

### Example 9: Find LINQ Query Chains

**Pattern:**
```
$collection$.Where($predicate1$).Select($selector$).ToList()
```

**What it matches:**
```csharp
users.Where(u => u.IsActive).Select(u => u.Name).ToList()           // ✓
items.Where(i => i.Price > 100).Select(i => i.Description).ToList() // ✓
```

---

### Example 10: Find String Concatenation (Old Style)

**Pattern:**
```
$var$ = $part1$ + $part2$ + $part3$;
```

**What it matches:**
```csharp
message = "Hello " + name + "!";          // ✓
path = folder + "/" + filename + ".txt";  // ✓
```

**Modern Replacement:**
```
$var$ = $"Hello {$part1$} {$part2$} {$part3$}";
```

---

## Constraint System Examples

### Type Constraints

**Exact Type:**
```
$var$:string = $value$;
```

**Base Type Match (using namespace pattern):**
```
$type:[System\.Collections\..*]$ items;
```

**Interface Implementation:**
```
public $type:[.*IDisposable.*]$ resource;
```

### Identifier Constraints

**Pascal Case (class/method names):**
```
public $method:[^[A-Z][a-zA-Z0-9]*$]$ $args$
```

**Camel Case (parameter/variable names):**
```
$var:[^[a-z][a-zA-Z0-9]*$]$ = new Object();
```

**Prefix Matching:**
```
$handler:[^On.*$]$ = null;
```

### Count Constraints

**Exact Arguments:**
```
Console.WriteLine($args:1$)
```

**Variable Range:**
```
$method$($args:1..10$)
```

**Multiple Statements:**
```
try
{
    $statements:1..∞$
}
catch
{
    $error$
}
```

---

## Real-World Use Cases

### Use Case 1: Migrate from XmlDocument to XDocument

**Problem:** Modernize legacy XML handling code

**Search Pattern:**
```
XmlDocument $doc$ = new XmlDocument();
```

**Replace Pattern:**
```
var $doc$ = new XDocument();
```

**Code Before:**
```csharp
public void LoadConfig(string path)
{
    XmlDocument config = new XmlDocument();
    config.Load(path);
    ProcessConfig(config);
}
```

**Code After:**
```csharp
public void LoadConfig(string path)
{
    var config = new XDocument.Load(path);
    ProcessConfig(config);
}
```

---

### Use Case 2: Add ConfigureAwait to Async Calls

**Search Pattern:**
```
await $task$;
```

**Constraint:** Only in library code (not UI)

**Replace Pattern:**
```
await $task$.ConfigureAwait(false);
```

**Benefit:** Improves async code performance in libraries

---

### Use Case 3: Detect Missing Using Statements

**Search Pattern:**
```
IDisposable $resource$ = new $class$($args$);
$statements$
```

**Finding:** Resources not wrapped in using statement

**Fix:** 
```
using (IDisposable $resource$ = new $class$($args$))
{
    $statements$
}
```

---

### Use Case 4: Find Unvalidated User Input Usage

**Search Pattern:**
```
var $input$ = Request.Query[$key$];
$usage$
```

**Finding:** Potential security vulnerability (unsanitized input)

---

### Use Case 5: Update to New Logging Pattern

**Search Pattern (Old):**
```
logger.LogInformation($message$);
```

**Replace Pattern (New):**
```
logger.LogInformation("Operation: {0}", $message$);
```

---

### Use Case 6: Find Unused Variable Assignments

**Search Pattern:**
```
$type$ $var$ = $value$;
return;
```

**Finding:** Assignments before early returns

---

## Command-Line Usage Examples

### Search Only

```bash
SharpCodeSearch.exe search \
  --pattern "$obj$.ToString()" \
  --workspace "C:\MyProject" \
  --format json \
  > results.json
```

### Search with Output Limit

```bash
SharpCodeSearch.exe search \
  --pattern "new $service:[.*Service$]$($args$)" \
  --workspace "C:\MyProject" \
  --max-results 100 \
  --format json
```

### Preview Replacements

```bash
SharpCodeSearch.exe replace \
  --pattern "if ($cond$) { return true; } else { return false; }" \
  --replacement "return $cond$;" \
  --workspace "C:\MyProject" \
  --preview-only \
  --format json
```

### Apply Replacements

```bash
SharpCodeSearch.exe replace \
  --pattern "XmlDocument $doc$ = new XmlDocument();" \
  --replacement "var $doc$ = new XDocument();" \
  --workspace "C:\MyProject" \
  --apply \
  --backup
```

---

## VS Code Extension Usage

### Opening Search Dialog

**Keyboard Shortcut:**
```
Ctrl+Alt+S (or Cmd+Alt+S on Mac)
```

**Via Command Palette:**
```
Ctrl+Shift+P → "SharpCodeSearch: Open Search"
```

### Workflow

1. Open search dialog
2. Enter pattern with placeholders
3. (Optional) Adjust constraints
4. Click "Search" or press Enter
5. View results in results panel
6. Click result to navigate
7. (Optional) Click "Replace" to replace single match

### Pattern Library

**Access:**
```
Ctrl+Shift+P → "SharpCodeSearch: Open Pattern Library"
```

**Features:**
- Browse saved patterns
- Quick search to find patterns
- Run pattern directly
- Edit pattern
- Share pattern (export/import)

---

## Troubleshooting Common Issues

### Issue: Pattern doesn't match expected code

**Possible Causes:**
1. Whitespace differences (resolved - ignored in semantic matching)
2. Different operators (e.g., `++x` vs `x++`)
3. Type not matching constraint
4. Extra parentheses or nesting

**Solution:**
- Use pattern builder UI to visualize AST
- Check constraint definitions
- Simplify pattern first, then add complexity
- Check code formatting

### Issue: Search is very slow

**Possible Causes:**
1. Large workspace (many files)
2. Complex pattern (expensive matching)
3. Compilation errors in project
4. Insufficient memory

**Solution:**
- Limit search scope (specific folder)
- Simplify pattern
- Close other applications
- Check project compiles correctly

### Issue: Replacement produces incorrect output

**Possible Causes:**
1. Placeholder names wrong
2. Escaping issue
3. Type mismatch in replacement

**Solution:**
- Use preview mode first
- Check placeholder names match search pattern
- Test on simple examples first

---

## Tips & Tricks

### Tip 1: Use Pattern Builder for Complex Patterns
- Visual editor helps understand AST structure
- Real-time preview shows what matches
- Constraint builder UI guides you

### Tip 2: Start Simple and Add Constraints
1. First pattern: `$method$($args$)`
2. Add type: `$method:[.*Get.*$]$($args$)`
3. Add constraints: `$args:0..1$`

### Tip 3: Test on Small Files First
- Create test file with known patterns
- Verify pattern works before full search
- Catch issues early

### Tip 4: Use Preview Before Batch Replace
- Always use preview mode for replacements
- Review changes before applying
- Save backup of files

### Tip 5: Organize Patterns by Category
- Use tags: refactoring, security, performance
- Add clear descriptions
- Share with team

### Tip 6: Leverage Search Scopes
- Search specific folder for large projects
- Use file patterns: `src/**/*.cs`
- Narrow scope for faster results

---

## Pattern Performance Tips

**Performance-Friendly Patterns:**
```
// Fast ✓
$var$.ToString()
new $class$($args$)
public $type$ $field$;
```

**Slow Patterns ✗ (avoid if possible):**
```
// Slow - matches everything
$expr$

// Slow - too general
$statements:1..∞$

// Slow - complex constraints
$var:[complex_regex]$ = new $class:[very_complex_pattern]$();
```

---

## Contributing Patterns to Library

**Guidelines:**
1. Pattern should solve real problem
2. Clear description of use case
3. Example code in search/replace
4. Performance tested
5. Include when/why to use

**Format:**
```json
{
  "name": "Find Unhandled Tasks",
  "description": "Detects fire-and-forget async calls that aren't awaited",
  "category": ["async", "bug-prevention"],
  "difficulty": "intermediate",
  "search": "$task:.*Task.*$;",
  "replace": "await $task$;",
  "examples": [
    {
      "before": "userService.SaveAsync(user);",
      "after": "await userService.SaveAsync(user);"
    }
  ],
  "author": "Your Name",
  "contributed": "2024-02-04"
}
```

---

## Next Steps

1. **Set up development environment** using prerequisites above
2. **Explore examples** to understand pattern syntax
3. **Build and test** the extension locally
4. **Create your own patterns** for your projects
5. **Share patterns** with your team
6. **Contribute** improvements back to project

---

## Resources

- **Roslyn Documentation:** https://github.com/dotnet/roslyn
- **VS Code Extension API:** https://code.visualstudio.com/api
- **ReSharper Patterns:** https://www.jetbrains.com/help/resharper/Navigation_and_Search__Structural_Search_and_Replace.html
- **Pattern Syntax Guide:** See ARCHITECTURE.md for detailed syntax
- **GitHub Issues:** Report bugs and request features

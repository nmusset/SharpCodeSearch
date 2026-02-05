# SharpCodeSearch - Usage Examples

This guide provides practical, ready-to-run examples for using SharpCodeSearch.

---

## CLI Usage Examples

### Basic Setup

Build the backend first:
```bash
cd src/backend
dotnet build
```

### ⚠️ Important: Shell-Specific Syntax

**PowerShell (Windows):**
- Use **single quotes** for patterns: `'Console.WriteLine($arg$)'`
- Double quotes cause variable expansion and will break patterns

**Bash/Zsh (Linux/Mac):**
- Use single or double quotes: `"Console.WriteLine($arg$)"`
- Single quotes recommended for consistency

**Examples below use PowerShell syntax** (suitable for Linux/Mac with minor adjustments)

---

## Example 1: Find All Console Output

### Pattern
```powershell
dotnet run -- --pattern 'Console.WriteLine($arg$)' --file YourFile.cs
```

### Sample C# Code (test.cs)
```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello World");
        Console.WriteLine("Debug info");
        var message = "Test";
        Console.WriteLine(message);
    }
}
```

### Expected Result
Finds all 3 `Console.WriteLine` calls and extracts the arguments:
```
Found 3 match(es):

test.cs:7:9
  Console.WriteLine("Hello World")
  Placeholders:
    arg = "Hello World"

test.cs:8:9
  Console.WriteLine("Debug info")
  Placeholders:
    arg = "Debug info"

test.cs:10:9
  Console.WriteLine(message)
  Placeholders:
    arg = message
```

**Note**: Both `Console.WriteLine($arg$)` and `WriteLine($arg$)` work.

---

## Example 2: Find Arithmetic Operations

### Pattern
```powershell
dotnet run -- --pattern '$x$ + $y$' --file Calculator.cs --output text
```

### Sample C# Code (Calculator.cs)
```csharp
class Calculator
{
    int Add(int a, int b)
    {
        return a + b;
    }
    
    void Process()
    {
        int sum = 5 + 10;
        int total = sum + 15;
    }
}
```

### Expected Result
Finds both addition operations and captures left/right operands:
- Match 1: `a + b` (left=a, right=b)
- Match 2: `5 + 10` (left=5, right=10)
- Match 3: `sum + 15` (left=sum, right=15)

---

## Example 3: Find Method Calls

### Pattern
```powershell
dotnet run -- --pattern '$obj$.ToString()' --file DataModel.cs
```

### Sample C# Code (DataModel.cs)
```csharp
class DataModel
{
    void PrintData()
    {
        var id = 123;
        var name = "User";
        
        Console.WriteLine(id.ToString());
        Console.WriteLine(name.ToString());
    }
}
```

### Expected Result
Finds:
- `id.ToString()` (obj=id)
- `name.ToString()` (obj=name)

---

## Example 4: Find Variable Declarations

### Pattern
```powershell
dotnet run -- --pattern 'var $name$ = $value$;' --file Variables.cs
```

### Sample C# Code (Variables.cs)
```csharp
class Variables
{
    void Method()
    {
        var count = 10;
        var message = "Hello";
        var result = Calculate();
    }
}
```

### Expected Result
Finds all `var` declarations and extracts variable names and initializers.

---

## Example 5: Find With Regex Constraint

### Pattern
```bash
dotnet run -- --pattern '$var:regex=temp.*$ = $value$' --file Config.cs
```

### Sample C# Code (Config.cs)
```csharp
class Config
{
    void LoadSettings()
    {
        var temporaryFile = "temp.txt";
        var tempDirectory = "/tmp";
        var temperature = 25;
        var config = "app.config";  // Won't match
    }
}
```

### Expected Result
Finds only variables starting with "temp":
- `temporaryFile`
- `tempDirectory`
- `temperature`

---

## Example 6: JSON Output

### Pattern (with JSON output)
```powershell
dotnet run -- --pattern 'Console.WriteLine($msg$)' --file App.cs
```

### Output Format
```json
{
  "matchCount": 2,
  "matches": [
    {
      "filePath": "App.cs",
      "line": 5,
      "column": 9,
      "matchedCode": "Console.WriteLine(\"Hello\")",
      "placeholders": {
        "msg": "\"Hello\""
      }
    },
    {
      "filePath": "App.cs",
      "line": 6,
      "column": 9,
      "matchedCode": "Console.WriteLine(\"World\")",
      "placeholders": {
        "msg": "\"World\""
      }
    }
  ]
}
```

**Note**: Argument placeholders capture the arguments without parentheses.

---

## Example 7: Find Binary Expressions

### Pattern
```powershell
dotnet run -- --pattern '$a$ == $b$' --file Comparisons.cs --output text
```

### Sample C# Code (Comparisons.cs)
```csharp
class Comparisons
{
    bool Compare(int x, int y)
    {
        if (x == y) return true;
        return x == 0 || y == 0;
    }
}
```

### Expected Result
Finds:
- `x == y`
- `x == 0`
- `y == 0`

---

## Example 8: Find Specific Method Calls

### Pattern
```powershell
dotnet run -- --pattern 'string.Format($args$)' --file StringOps.cs
```

### Sample C# Code (StringOps.cs)
```csharp
class StringOps
{
    void FormatStrings()
    {
        var s1 = string.Format("Hello {0}", name);
        var s2 = string.Format("Value: {0}, {1}", x, y);
        var s3 = $"Interpolated: {name}";  // Won't match
    }
}
```

### Expected Result
Finds the two `string.Format` calls:
- Match 1: `args = "Hello {0}", name`
- Match 2: `args = "Value: {0}, {1}", x, y`

**Note**: Multiple arguments are captured as comma-separated values without the surrounding parentheses.

---

## Example 9: Find Property Access

### Pattern
```powershell
dotnet run -- --pattern '$obj$.Length' --file Arrays.cs
```

### Sample C# Code (Arrays.cs)
```csharp
class Arrays
{
    void ProcessArrays()
    {
        var arr = new int[] { 1, 2, 3 };
        var count = arr.Length;
        
        var str = "test";
        var len = str.Length;
    }
}
```

### Expected Result
Finds:
- `arr.Length`
- `str.Length`

---

## Example 10: Find If Conditions

### Pattern
```powershell
dotnet run -- --pattern 'if ($condition$)' --file Logic.cs
```

### Sample C# Code (Logic.cs)
```csharp
class Logic
{
    void CheckConditions(int x)
    {
        if (x > 0)
            DoSomething();
            
        if (x < 100)
        {
            DoOther();
        }
    }
}
```

### Expected Result
Finds both if statements and extracts conditions:
- `x > 0`
- `x < 100`

---

## Quick Test Script

Create this PowerShell script to test all examples:

```powershell
# test-patterns.ps1

# Create test file
$testCode = @"
using System;

class TestProgram
{
    static void Main()
    {
        Console.WriteLine("Hello");
        Console.WriteLine("World");
        int x = 5 + 10;
        var name = "Test";
        name.ToString();
    }
}
"@

Set-Content "TestFile.cs" $testCode

# Test 1: Find Console.WriteLine
Write-Host "`n=== Test 1: Console.WriteLine ===" -ForegroundColor Cyan
dotnet run --project src/backend -- --pattern 'Console.WriteLine($arg$)' --file TestFile.cs --output text

# Test 2: Find additions
Write-Host "`n=== Test 2: Additions ===" -ForegroundColor Cyan
dotnet run --project src/backend -- --pattern '$x$ + $y$' --file TestFile.cs --output text

# Test 3: Find ToString calls
Write-Host "`n=== Test 3: ToString calls ===" -ForegroundColor Cyan
dotnet run --project src/backend -- --pattern '$obj$.ToString()' --file TestFile.cs --output text

# Cleanup
Remove-Item "TestFile.cs"
```

Run it:
```powershell
pwsh test-patterns.ps1
```

---

## Using from VS Code Extension

1. Open Command Palette: `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
2. Type: `Sharp Code Search: Search`
3. Enter your pattern: `Console.WriteLine($arg$)`
4. View results in the sidebar

---

## Tips & Best Practices

### Pattern Design
- Start simple, add complexity gradually
- Use descriptive placeholder names
- Test patterns on small files first

### Performance
- Search single files first before workspace-wide
- Use constraints to narrow results
- Patterns are case-sensitive

### Debugging Patterns
- Use `--output text` for human-readable output
- Use `--output json` for machine processing
- Check placeholder capture with simple patterns first

---

## Common Patterns Library

```bash
# Find all LINQ queries
dotnet run -- --pattern '$collection$.Where($predicate$)' --file Data.cs

# Find null checks
dotnet run -- --pattern 'if ($var$ == null)' --file Safety.cs

# Find string concatenation
dotnet run -- --pattern '$str1$ + $str2$' --file Strings.cs

# Find array access
dotnet run -- --pattern '$array$[$index$]' --file Arrays.cs

# Find type casts
dotnet run -- --pattern '($type$)$expr$' --file Casts.cs
```

---

## Next Steps

- Read [QUICK_START_AND_EXAMPLES.md](Docs/QUICK_START_AND_EXAMPLES.md) for advanced patterns
- See [ARCHITECTURE.md](Docs/ARCHITECTURE.md) for technical details
- Check [ROADMAP.md](Docs/ROADMAP.md) for upcoming features

---

**Happy Pattern Matching!** 🎯

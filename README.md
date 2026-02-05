# SharpCodeSearch

**Semantic pattern-based search and replace for C# codebases in VS Code**

SharpCodeSearch is a VS Code extension that brings powerful semantic code search to C# development, similar to ReSharper's Structural Search feature. Instead of simple text search, it understands C# code structure and allows you to find patterns using placeholders.

---

## 🚀 Features

- **Pattern-Based Search**: Use placeholders to match code patterns
- **Semantic Understanding**: Powered by Roslyn for accurate C# code analysis
- **Placeholder System**: Match expressions, identifiers, and more
- **Constraint Support**: Filter matches with regex and other constraints
- **JSON/Text Output**: Get results in your preferred format

---

## 📦 Installation

### Prerequisites

- **VS Code**: Version 1.95.0 or higher
- **.NET 10 SDK**: Required for the backend
- **Node.js 20+**: For building the extension

### Build from Source

```bash
# Clone the repository
git clone https://github.com/yourusername/SharpCodeSearch.git
cd SharpCodeSearch

# Build the backend
cd src/backend
dotnet build

# Build the extension
cd ../extension
npm install
npm run compile

# Run tests
cd ../tests
dotnet test
```

---

## 🎯 Quick Start

### Using the CLI

The backend can be used directly from the command line:

**PowerShell (Windows):**
```powershell
# Search for a pattern in a C# file - use SINGLE quotes!
dotnet run --project src/backend -- --pattern 'Console.WriteLine($arg$)' --file YourFile.cs

# Get results in text format
dotnet run --project src/backend -- --pattern '$x$ + $y$' --file Calculator.cs --output text
```

**Bash/Zsh (Linux/Mac):**
```bash
# Single or double quotes work
dotnet run --project src/backend -- --pattern "Console.WriteLine($arg$)" --file YourFile.cs
```

> **⚠️ Important**: In PowerShell, always use **single quotes** for patterns with placeholders. Double quotes cause PowerShell variable expansion and will break your patterns.

### Using the VS Code Extension

1. Open a C# project in VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
3. Type "Sharp Code Search: Search"
4. Enter your search pattern
5. View results in the search panel

---

## 📝 Pattern Syntax

### Basic Patterns

SharpCodeSearch uses patterns with placeholders to match code:

- **Simple text**: `Console.WriteLine` - Matches literal text
- **Placeholders**: `$identifier$` - Matches any expression/identifier
- **Combined**: `$obj$.ToString()` - Matches method calls on any object

### Placeholder Types

| Placeholder | Matches | Example Pattern | Example Match |
|-------------|---------|----------------|---------------|
| `$expr$` | Any expression | `if ($expr$)` | `if (x > 5)` |
| `$id$` | Identifiers | `$id$ = 10` | `count = 10` |
| `$var$` | Variables | `var $var$ = $expr$` | `var result = calc()` |
| `$obj$` | Objects | `$obj$.Method()` | `user.Save()` |
| `$arg$` | Arguments | `Method($arg$)` | `Method(value)` |

### Constraints

Add constraints to filter matches:

```
$var:regex=name.*$ = $value$
```

This pattern matches variable assignments where the variable name starts with "name".

**Supported constraints:**
- `regex=<pattern>` - Match placeholder value with regex
- `type=<typename>` - Match specific types (requires semantic model)
- `count=<n>` - Match specific number of items

### Example Patterns

```csharp
// Find all Console.WriteLine calls
Console.WriteLine($arg$)

// Find binary operations
$left$ + $right$

// Find method calls with specific pattern
$obj$.ToString()

// Find variable declarations
var $name$ = $value$;

// Find if statements with conditions
if ($condition$) { $body$ }
```

---

## 🔧 Usage Examples

### Example 1: Find All Console Output

**Pattern:**
```
Console.WriteLine($message$)
```

**Matches:**
```csharp
Console.WriteLine("Hello");        // ✓
Console.WriteLine(userName);       // ✓
Console.WriteLine($"Value: {x}");  // ✓
```

### Example 2: Find Arithmetic Operations

**Pattern:**
```
$x$ + $y$
```

**Matches:**
```csharp
int sum = a + b;          // ✓
result = 5 + 10;          // ✓
var total = count + 1;    // ✓
```

### Example 3: Find ToString Calls

**Pattern:**
```
$obj$.ToString()
```

**Matches:**
```csharp
user.ToString();          // ✓
value.ToString();         // ✓
item.ToString();          // ✓
```

### Example 4: Constrained Search

**Pattern:**
```
$var:regex=temp.*$ = $value$
```

**Matches:**
```csharp
var tempValue = 10;       // ✓ (var name starts with "temp")
int temperature = 20;     // ✓ (var name starts with "temp")
var result = 30;          // ✗ (var name doesn't match)
```

---

## 🏗️ Project Structure

```
SharpCodeSearch/
├── src/
│   ├── backend/              # C# pattern matching engine
│   │   ├── Services/         # Pattern parser, matcher, validators
│   │   ├── Models/           # Pattern AST, placeholder types
│   │   ├── Roslyn/           # Roslyn integration
│   │   ├── Caching/          # Compilation caching
│   │   └── Program.cs        # CLI entry point
│   ├── extension/            # VS Code extension
│   │   ├── src/              # Extension TypeScript code
│   │   │   ├── extension.ts  # Extension entry point
│   │   │   ├── BackendService.ts
│   │   │   └── SearchCommand.ts
│   │   └── webview/          # Search UI
│   └── tests/                # Backend unit & integration tests
├── Docs/                     # Documentation
│   ├── ROADMAP.md
│   ├── ARCHITECTURE.md
│   └── QUICK_START_AND_EXAMPLES.md
└── README.md                 # This file
```

---

## 🧪 Testing

### Backend Tests

```bash
cd src/tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true
```

**Test Coverage: 142 tests passing** ✅

### Extension Tests

```bash
cd src/extension
npm test
```

---

## 🛠️ Development

### Setup Development Environment

1. Clone the repository
2. Install prerequisites (.NET 10, Node.js 20+)
3. Build both backend and extension (see Installation)
4. Open in VS Code
5. Press F5 to launch extension in debug mode

### Running in Debug Mode

- **Backend**: Use the `.NET Core Launch` configuration in VS Code
- **Extension**: Use `Extension` or `Extension Tests` configuration

### Project Configuration Files

- `.vscode/launch.json` - Debug configurations
- `.vscode/tasks.json` - Build tasks
- `src/backend/SharpCodeSearch.csproj` - Backend project file
- `src/extension/package.json` - Extension manifest
- `src/extension/tsconfig.json` - TypeScript configuration

---

## 📚 Documentation

For more detailed information, see:

- **[ROADMAP.md](Docs/ROADMAP.md)** - Development roadmap and phases
- **[ARCHITECTURE.md](Docs/ARCHITECTURE.md)** - Technical architecture
- **[QUICK_START_AND_EXAMPLES.md](Docs/QUICK_START_AND_EXAMPLES.md)** - Detailed examples
- **[TESTING_PHASE_1.4.md](Docs/TESTING_PHASE_1.4.md)** - Testing documentation

---

## 🎯 Current Status

**Phase 1 Complete** ✅

- ✅ CLI prototype functional
- ✅ Extension scaffolding complete
- ✅ Pattern parser working
- ✅ Basic pattern matching implemented
- ✅ Webview UI operational
- ✅ Backend/extension communication working
- ✅ 142 unit & integration tests passing
- ✅ Test coverage >80%

**Next Steps: Phase 2**
- Full placeholder type support (statements, arguments, types)
- Workspace-level search
- Search & replace functionality
- Pattern catalog system

---

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Workflow

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Run all tests
6. Submit a pull request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Roslyn** - Microsoft's .NET Compiler Platform
- **VS Code Extension API** - For extension infrastructure
- **ReSharper** - Inspiration for structural search

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/SharpCodeSearch/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/SharpCodeSearch/discussions)

---

## 📊 Tech Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Backend | C# (.NET 10) | Pattern matching engine |
| Code Analysis | Roslyn | C# syntax/semantic analysis |
| Extension | TypeScript | VS Code extension |
| Frontend | HTML/CSS/JS | Search UI webview |
| Testing | xUnit, Mocha | Test frameworks |
| Build | .NET CLI, npm | Build tooling |

---

**Made with ❤️ for C# developers**

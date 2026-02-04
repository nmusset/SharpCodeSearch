# SharpCodeSearch - Visual Reference Guide

## System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                 VS CODE EDITOR                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  SIDEBAR PANEL (Webview)                                            │  │
│  │  ┌──────────────────────────────────────────────────────────────┐  │  │
│  │  │ Search Pattern Input                                         │  │  │
│  │  │ ┌────────────────────────────────────────────────────────┐  │  │  │
│  │  │ │ Enter pattern: $obj$.ToString()                        │  │  │  │
│  │  │ │ Options | Constraints | Advanced                        │  │  │  │
│  │  │ └────────────────────────────────────────────────────────┘  │  │  │
│  │  ├──────────────────────────────────────────────────────────────┤  │  │
│  │  │ Search Results                                             │  │  │
│  │  │ ┌──────────────────────────────────────────────────────┐  │  │  │
│  │  │ │ Matches: 42                                          │  │  │  │
│  │  │ │ Time: 1.2s                                           │  │  │  │
│  │  │ ├─ UserService.cs:45                                   │  │  │  │
│  │  │ │  ├─ user.ToString()                                  │  │  │  │
│  │  │ │  └─ captured: obj=user                               │  │  │  │
│  │  │ ├─ OrderService.cs:128                                 │  │  │  │
│  │  │ │  ├─ order.ToString()                                 │  │  │  │
│  │  │ │  └─ captured: obj=order                              │  │  │  │
│  │  │ └─ ...40 more matches                                  │  │  │  │
│  │  │                                                         │  │  │  │
│  │  │ [Replace] [Preview] [Copy] [Export]                   │  │  │  │
│  │  └──────────────────────────────────────────────────────┘  │  │  │
│  │  ├──────────────────────────────────────────────────────────┤  │  │
│  │  │ Pattern Catalog                                         │  │  │
│  │  │ ┌──────────────────────────────────────────────────────┐  │  │  │
│  │  │ │ Search saved patterns                                │  │  │  │
│  │  │ │ - Find ToString() calls                              │  │  │  │
│  │  │ │ - Detect anti-patterns                               │  │  │  │
│  │  │ │ - Migrate XmlDocument                                │  │  │  │
│  │  │ │ [+ New] [Edit] [Delete]                              │  │  │  │
│  │  │ └──────────────────────────────────────────────────────┘  │  │  │
│  │  └──────────────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘
│                                 │
│                    ┌────────────▼────────────┐
│                    │  Extension Commands     │
│                    │  • Search               │
│                    │  • Replace              │
│                    │  • Open Catalog         │
│                    └────────────┬────────────┘
│                                 │
└─────────────────────────────────┼──────────────────────────────────────────┘
                                  │
                    ┌─────────────▼──────────────┐
                    │  Communication Layer      │
                    │  (Process Execution)      │
                    │                           │
                    │  SharpCodeSearch.exe      │
                    │  --pattern "..."          │
                    │  --workspace "..."        │
                    │                           │
                    │  Returns: JSON            │
                    └─────────────┬──────────────┘
                                  │
┌─────────────────────────────────┼──────────────────────────────────────────┐
│                                 │                                          │
│                    ┌────────────▼──────────────┐                          │
│                    │  .NET Backend Process     │                          │
│                    │                           │                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ Analysis Engine                                                    │  │
│  │                                                                    │  │
│  │  INPUT: Pattern String                                            │  │
│  │  │                                                                │  │
│  │  ├─▶ Pattern Parser                                              │  │
│  │  │   ├─ Tokenize: "$obj$.ToString()"                            │  │
│  │  │   ├─ Parse: TextNode + PlaceholderNode + InvocationNode      │  │
│  │  │   └─ Validate: All placeholders defined                      │  │
│  │  │                                                                │  │
│  │  ├─▶ Workspace Scanner                                           │  │
│  │  │   ├─ Find all .cs files                                       │  │
│  │  │   ├─ Load .csproj file                                        │  │
│  │  │   └─ Build Compilation context (Roslyn)                      │  │
│  │  │                                                                │  │
│  │  ├─▶ Pattern Matcher                                             │  │
│  │  │   ├─ For each SyntaxTree:                                     │  │
│  │  │   │  ├─ Traverse SyntaxNodes (DFS)                           │  │
│  │  │   │  ├─ Compare with pattern                                  │  │
│  │  │   │  ├─ For placeholders: extract values                      │  │
│  │  │   │  └─ Collect matches                                       │  │
│  │  │   │                                                            │  │
│  │  │   └─ For each match:                                          │  │
│  │  │      ├─ Validate constraints                                  │  │
│  │  │      ├─ Get semantic info                                     │  │
│  │  │      └─ Report match                                          │  │
│  │  │                                                                │  │
│  │  ├─▶ Constraint Validator                                        │  │
│  │  │   ├─ Type constraints (Roslyn semantic model)                │  │
│  │  │   ├─ Regex constraints                                        │  │
│  │  │   ├─ Count constraints                                        │  │
│  │  │   └─ Semantic constraints (modifiers, attributes)            │  │
│  │  │                                                                │  │
│  │  └─▶ Result Collector & Formatter                                │  │
│  │      ├─ Collect all matches                                      │  │
│  │      ├─ Extract context (surrounding code)                       │  │
│  │      ├─ Serialize to JSON                                        │  │
│  │      └─ OUTPUT: JSON Results                                     │  │
│  │                                                                    │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ Roslyn Integration Layer                                           │  │
│  │                                                                    │  │
│  │  ┌──────────────────┐      ┌──────────────────┐                  │  │
│  │  │ SyntaxTree       │      │ SemanticModel    │                  │  │
│  │  │ Navigator        │      │ Type Resolution  │                  │  │
│  │  │                  │      │ Symbol Info      │                  │  │
│  │  │ DFS Traversal    │      │ Inheritance Checks                 │  │
│  │  │ Node Matching    │      │ Generic Support  │                  │  │
│  │  └──────────────────┘      └──────────────────┘                  │  │
│  │                                                                    │  │
│  │  Roslyn NuGet Packages:                                           │  │
│  │  • Microsoft.CodeAnalysis                                         │  │
│  │  • Microsoft.CodeAnalysis.CSharp                                  │  │
│  │  • Microsoft.CodeAnalysis.Workspaces.MSBuild                     │  │
│  │                                                                    │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ Caching Layer                                                      │  │
│  │                                                                    │  │
│  │  Level 1: Pattern Cache        Level 2: Compilation Cache        │  │
│  │  ┌──────────────────────┐      ┌──────────────────────┐          │  │
│  │  │ Pattern AST Cache    │      │ Workspace Compilation│          │  │
│  │  │ Key: Hash(pattern)   │      │ Key: WorkspacePath   │          │  │
│  │  │ TTL: Session         │      │ TTL: File mod time   │          │  │
│  │  └──────────────────────┘      └──────────────────────┘          │  │
│  │                                                                    │  │
│  │  Level 3: Result Cache                                            │  │
│  │  ┌──────────────────────────────────────────┐                    │  │
│  │  │ Results Cache                            │                    │  │
│  │  │ Key: Hash(pattern+workspace+files)       │                    │  │
│  │  │ TTL: 5 minutes                           │                    │  │
│  │  └──────────────────────────────────────────┘                    │  │
│  │                                                                    │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Pattern Matching Algorithm Flow

```
START: Match Pattern Against Code
│
├─ INPUT: PatternNode, SyntaxNode
│
├─ IF PatternNode is TextNode
│  └─ Check if node.ToString() == pattern.Text
│     ├─ TRUE  → Continue to next node
│     └─ FALSE → Backtrack
│
├─ IF PatternNode is PlaceholderNode
│  ├─ Check constraints (type, regex, count)
│  │  ├─ Valid   → captures[name] = node; Continue
│  │  └─ Invalid → Backtrack
│  └─ Handle count (single, min..max)
│
├─ IF PatternNode is ComplexNode (contains children)
│  ├─ FOR EACH child PatternNode
│  │  ├─ Find corresponding child SyntaxNode
│  │  ├─ Recursively: Match child pattern vs child syntax
│  │  │  ├─ MATCH   → Continue
│  │  │  └─ NO MATCH → Backtrack and try next node
│  │  └─ Continue to next child
│  └─ ALL children matched → SUCCESS
│
└─ OUTPUT: Match object with captures
   │
   ├─ matched_node: SyntaxNode
   ├─ captures: {placeholder_name → code_snippet}
   ├─ location: {file, line, column}
   └─ context: surrounding code for display
```

---

## Pattern to Regex Constraint Example

```
Pattern Input String:
$method:[^Get.*$]$($args:1..2$)

Parsing Flow:
│
├─ Tokenize: ["$", "method", ":", "[^Get.*$]", "$", "(", "$", "args", ...
│
├─ Identify Placeholders:
│  ├─ Placeholder: "method"
│  │  ├─ Type: Identifier
│  │  └─ Constraint: RegexConstraint("[^Get.*$]")
│  │
│  └─ Placeholder: "args"
│     ├─ Type: Argument
│     └─ Constraint: CountConstraint(min=1, max=2)
│
└─ Build PatternAST:
   │
   ├─ PlaceholderNode("method")
   │  └─ constraints: [RegexConstraint(pattern="^Get.*$")]
   │
   ├─ TextNode("(")
   │
   ├─ PlaceholderNode("args")
   │  └─ constraints: [CountConstraint(min=1, max=2)]
   │
   └─ TextNode(")")

During Matching:
│
├─ Encounter: GetUserName(context)
│
├─ Match "method" placeholder:
│  ├─ Extract identifier: "GetUserName"
│  ├─ Apply regex: "^Get.*$"
│  ├─ Test: "GetUserName" matches "^Get.*$" ✓
│  └─ captures["method"] = "GetUserName"
│
├─ Match "args" placeholder:
│  ├─ Count arguments: 1
│  ├─ Apply count constraint: 1 in range [1..2] ✓
│  └─ captures["args"] = "context"
│
└─ MATCH FOUND ✓
```

---

## Data Flow: Search Operation

```
User Types Pattern and Clicks Search
│
▼
VS Code Extension (TypeScript)
├─ Validate pattern format
├─ Show loading indicator
└─ Send HTTP request to backend
   │
   ▼
.NET Backend Process Receives Request
├─ Parse JSON: { command: "search", pattern: "..." }
├─ Cache check: Is pattern already parsed?
│  ├─ YES → Use cached PatternAST
│  └─ NO  → Parse pattern string → Build PatternAST
│
├─ Cache check: Compilation for workspace?
│  ├─ YES → Check if files modified
│  │  ├─ NO → Use cached compilation
│  │  └─ YES → Rebuild compilation
│  └─ NO → Build compilation from .csproj
│
├─ Initialize matcher
├─ FOR EACH C# file in workspace
│  ├─ Get SyntaxTree from compilation
│  ├─ Get SemanticModel from compilation
│  ├─ Create PatternMatcher instance
│  ├─ Walk through SyntaxTree (DFS)
│  └─ FOR EACH SyntaxNode
│     ├─ Try to match against pattern
│     ├─ IF match → Validate constraints using SemanticModel
│     └─ IF all constraints valid → Add to results
│
├─ Collect all matches
├─ Format results as JSON
└─ Send response back to VS Code
   │
   ▼
VS Code Extension Receives Results
├─ Parse JSON response
├─ Hide loading indicator
├─ Display matches count: "Found 42 matches"
├─ Populate results tree
│  ├─ Group by file
│  ├─ Show line:col for each match
│  └─ Show code snippet
└─ Enable navigation/replace buttons
   │
   ▼
User Actions
├─ Click on result
│  └─ Extension: Navigate to file, highlight match
│
├─ Click "Replace" on single result
│  ├─ Extension: Send replace request to backend
│  ├─ Backend: Generate replacement code
│  ├─ Backend: Validate replacement syntax
│  └─ Backend: Apply replacement to file
│
└─ Click "Replace All"
   ├─ Extension: Show preview of all replacements
   ├─ User confirms
   └─ Extension: Send batch replace request
      └─ Backend: Apply all replacements
```

---

## Constraint Validation Flow

```
SyntaxNode: Identifier "user"
Pattern: $obj:[IEnumerable.*]$

Constraint Check:
│
├─ Get semantic info from SemanticModel
│  └─ ITypeSymbol userType = GetType("user")
│
├─ Check if userType matches constraint "IEnumerable.*"
│  ├─ Is userType exactly "IEnumerable"?
│  │  └─ NO
│  │
│  ├─ Does userType implement IEnumerable?
│  │  └─ NO
│  │
│  ├─ Does userType match regex "IEnumerable.*"?
│  │  ├─ userType.Name = "List`1"
│  │  └─ Pattern match → NO
│  │
│  └─ Does userType inherit from matching type?
│     ├─ Check base types
│     ├─ For List`1 → base is Collection -> base is ICollection -> base is IEnumerable ✓
│     └─ MATCH FOUND ✓
│
└─ Result: Constraint VALID ✓
```

---

## File Processing Stages

```
Workspace
│
├─ .csproj files found
│
├─ Load Project References
│  ├─ Parse MSBuild metadata
│  ├─ Resolve package dependencies
│  └─ Create Compilation
│
├─ Build SyntaxTrees
│  └─ FOR EACH .cs file
│     ├─ Parse source code
│     ├─ Generate SyntaxTree
│     └─ Add to Compilation
│
├─ Build SemanticModel
│  └─ Bind symbols in Compilation
│     ├─ Resolve type references
│     ├─ Check type compatibility
│     └─ Create symbol table
│
├─ Search Phase
│  └─ FOR EACH SyntaxTree
│     ├─ Get SemanticModel
│     ├─ Traverse SyntaxTree
│     ├─ Match nodes
│     └─ Collect results
│
└─ Result Aggregation
   ├─ Group by file
   ├─ Sort by location
   ├─ De-duplicate
   └─ Format for display
```

---

## Placeholder Type Examples

```
EXPRESSION PLACEHOLDER ($expr$)
┌─────────────────────────┐
│ Pattern: x = $expr$;    │
├─────────────────────────┤
│ Matches:                │
│ ✓ x = 5;                │
│ ✓ x = foo();            │
│ ✓ x = a + b;            │
│ ✓ x = obj.Method();     │
│ ✓ x = new Class();      │
└─────────────────────────┘

IDENTIFIER PLACEHOLDER ($id$)
┌─────────────────────────┐
│ Pattern:                │
│ public $id$ Method()    │
├─────────────────────────┤
│ Matches:                │
│ ✓ public void M()       │
│ ✓ public int Count()    │
│ ✗ public void m()       │ (lowercase)
└─────────────────────────┘

STATEMENT PLACEHOLDER ($stmt$)
┌──────────────────────────────┐
│ Pattern:                     │
│ try { $stmt$ } catch { }     │
├──────────────────────────────┤
│ Matches:                     │
│ ✓ try { x++; } catch { }    │
│ ✓ try { Foo(); } catch { }  │
│ ✗ try { } catch { }         │ (no stmt)
└──────────────────────────────┘

ARGUMENT PLACEHOLDER ($args$)
┌──────────────────────────┐
│ Pattern: M($args$)       │
├──────────────────────────┤
│ Matches:                 │
│ ✓ M()                    │
│ ✓ M(5)                   │
│ ✓ M(1, "x", obj)        │
│ ✗ Never: syntax error   │
└──────────────────────────┘

TYPE PLACEHOLDER ($type$)
┌────────────────────────────┐
│ Pattern: $type$ x;         │
├────────────────────────────┤
│ Matches:                   │
│ ✓ int x;                   │
│ ✓ string x;                │
│ ✓ IEnumerable<T> x;       │
│ ✓ List<string> x;         │
└────────────────────────────┘
```

---

## Performance Characteristics

```
Operation              | Typical Time | Factors
─────────────────────────────────────────────────────────────
Parse 1 pattern        | <10ms        | Pattern complexity
Build compilation      | 1-5s         | Project size, deps
Search 100 files       | 100-500ms    | Pattern complexity
Search 1000 files      | 1-5s         | Pattern complexity
Search 10000 files     | 10-30s       | Pattern complexity
Single replacement     | <50ms        | Replacement size
Batch replace (100)    | 500ms-2s     | Total code size
─────────────────────────────────────────────────────────────

Memory Usage:
• Pattern AST:          ~10-50 KB
• Compilation:          100-500 MB (depends on project)
• Search results:       100 KB - 1 MB
• Total per search:     ~100-600 MB
```

---

## Testing Pyramid

```
                    △
                   ╱ ╲
                  ╱   ╲  E2E Tests (10%)
                 ╱     ╲ • Full workflow
                ╱───────╲ • Real projects
               ╱  △      ╲
              ╱   ╱ ╲     ╲
             ╱   ╱   ╲     ╲ Integration Tests (30%)
            ╱   ╱     ╲     ╲ • Component interaction
           ╱───╱───────╲─────╲ • Matcher + Validator
          ╱  △          △      ╲
         ╱   ╱ ╲        ╱ ╲     ╲
        ╱   ╱   ╲      ╱   ╲     ╲ Unit Tests (60%)
       ╱   ╱     ╲    ╱     ╲     ╲ • Parser tests
      ╱___╱───────╲__╱───────╲_____╲ • Matcher logic
                                    • Constraint validation
                                    • AST utilities
```

---

## Extension Lifecycle

```
User Opens VS Code
│
▼
Extension Host loads package.json
├─ Reads activation events: "onCommand:sharpCodeSearch.search"
├─ Extension stays inactive until event triggered
│
▼
User Triggers Command (Ctrl+Alt+S or palette)
│
▼
VS Code emits activation event
│
▼
Extension Host calls activate(context)
│
├─ Initialize components
├─ Register commands
├─ Load pattern catalog
├─ Set up file watchers
└─ Return extension object
│
▼
Extension becomes active
│
├─ Listen for commands
├─ Watch for configuration changes
└─ React to editor events
│
▼
User Runs Search
│
├─ Command handler receives pattern
├─ Spawns backend process
├─ Backend performs analysis
├─ Display results
└─ Listen for user navigation
│
▼
User Closes VS Code
│
▼
VS Code calls deactivate()
│
├─ Save pattern catalog
├─ Close backend process
├─ Clean up resources
└─ Dispose listeners
```

---

## Communication Protocol (JSON-RPC over CLI)

```
REQUEST:
───────
{
  "id": 1,
  "method": "search",
  "params": {
    "pattern": "$obj$.ToString()",
    "workspace": "/path/to/project",
    "scope": "src/**/*.cs",
    "options": {
      "timeout": 30000,
      "maxResults": 10000
    }
  }
}

RESPONSE:
────────
{
  "id": 1,
  "result": {
    "status": "success",
    "matches": [
      {
        "file": "/path/to/file.cs",
        "line": 45,
        "column": 20,
        "text": "user.ToString()",
        "context": "string name = user.ToString();",
        "captures": {
          "obj": "user"
        }
      },
      ... more matches ...
    ],
    "totalMatches": 42,
    "processingTimeMs": 1234
  }
}

ERROR RESPONSE:
──────────────
{
  "id": 1,
  "error": {
    "code": -1,
    "message": "Pattern parse error",
    "data": {
      "details": "Undefined placeholder: $unknown$",
      "position": 15
    }
  }
}
```

---

## Component Dependencies

```
┌─────────────────────────────────────────────────┐
│ VS Code Extension (UI Layer)                    │
│                                                 │
│ ├─ Commands                                     │
│ │  └─ depends on → BackendService              │
│ │                                               │
│ ├─ SearchPanel                                  │
│ │  └─ depends on → ResultDisplayManager        │
│ │     └─ depends on → Navigation API            │
│ │                                               │
│ ├─ PatternCatalog                              │
│ │  └─ depends on → PatternManager              │
│ │     └─ depends on → VS Code Storage API      │
│ │                                               │
│ └─ BackendService                              │
│    └─ depends on → Process/IPC                 │
│                                                 │
└──────────────────┬──────────────────────────────┘
                   │
              Process IPC
                   │
┌──────────────────▼──────────────────────────────┐
│ Backend Analysis Engine (.NET)                  │
│                                                 │
│ ├─ AnalysisService                             │
│ │  ├─ depends on → PatternParser               │
│ │  ├─ depends on → WorkspaceScanner            │
│ │  └─ depends on → PatternMatcher              │
│ │                                               │
│ ├─ PatternMatcher                              │
│ │  ├─ depends on → ConstraintValidator         │
│ │  ├─ depends on → RoslynHelper                │
│ │  └─ depends on → ReplacementEngine           │
│ │                                               │
│ ├─ ConstraintValidator                         │
│ │  └─ depends on → Roslyn SemanticModel        │
│ │                                               │
│ ├─ WorkspaceScanner                            │
│ │  └─ depends on → CompilationManager          │
│ │                                               │
│ └─ RoslynHelper                                │
│    └─ depends on → Roslyn APIs                 │
│       ├─ Microsoft.CodeAnalysis               │
│       ├─ Microsoft.CodeAnalysis.CSharp        │
│       └─ Microsoft.CodeAnalysis.Workspaces.MSBuild
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## Performance Optimization Strategies

```
1. MULTI-LEVEL CACHING
   ├─ Pattern AST Cache (in-memory)
   ├─ Compilation Cache (in-memory, with file watching)
   └─ Result Cache (in-memory, TTL-based)

2. INCREMENTAL ANALYSIS
   ├─ Track modified files
   ├─ Only re-parse modified files
   └─ Reuse unchanged parts of compilation

3. PARALLEL PROCESSING
   ├─ Process files in parallel (ThreadPool)
   ├─ Thread-safe result collection
   └─ Cancellation token support

4. SMART ALGORITHM
   ├─ Early termination for non-matching nodes
   ├─ Efficient AST traversal (DFS)
   └─ Lazy constraint evaluation

5. RESOURCE MANAGEMENT
   ├─ Memory pooling for temporary objects
   ├─ Disposal of large objects (compilation)
   └─ GC hints and optimization
```

---

This visual reference guide complements the detailed documentation. Use these diagrams to understand the overall architecture, data flow, and component relationships.

# SharpCodeSearch - Technical Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    VS Code Extension (TypeScript)           │
│  ┌────────────────┐  ┌─────────────────┐  ┌──────────────┐  │
│  │  Command       │  │  Search UI      │  │  Pattern     │  │
│  │  Dispatcher    │  │  (Webview)      │  │  Manager     │  │
│  └───────┬────────┘  └────────┬────────┘  └──────┬───────┘  │
│          │                    │                  │          │
│          └────────────────────┼──────────────────┘          │
│                               │                             │
│                    ┌──────────▼──────────┐                  │
│                    │ Extension Host API  │                  │
│                    │ (Event Loop)        │                  │
│                    └──────────┬──────────┘                  │
└───────────────────────────────┼─────────────────────────────┘
                                │
                  ┌─────────────▼──────────────┐
                  │  Communication Layer       │
                  │  (Process/Socket/Pipe)     │
                  └─────────────┬──────────────┘
                                │
┌───────────────────────────────┼─────────────────────────────┐
│         .NET Backend Process (.NET 10+)                     │
│                               │                             │
│  ┌────────────────────────────▼───────────────────────────┐ │
│  │              Analysis Engine                           │ │
│  │                                                        │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐   │ │
│  │  │ Pattern      │  │ AST Matcher  │  │  Workspace  │   │ │
│  │  │ Parser       │  │              │  │  Scanner    │   │ │
│  │  └──────────────┘  └──────────────┘  └─────────────┘   │ │
│  │                                                        │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐   │ │
│  │  │ Replacement  │  │  Constraint  │  │  Result     │   │ │
│  │  │ Engine       │  │  Validator   │  │  Collector  │   │ │
│  │  └──────────────┘  └──────────────┘  └─────────────┘   │ │
│  │                                                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              Roslyn Integration Layer                  │ │
│  │                                                        │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐   │ │
│  │  │  Compilation │  │  SyntaxTree  │  │ SemanticModel   │ │
│  │  │  Manager     │  │  Navigator   │  │                 │ │
│  │  └──────────────┘  └──────────────┘  └─────────────┘   │ │
│  │                                                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              Cache & Optimization Layer                │ │
│  │                                                        │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐   │ │
│  │  │  Pattern     │  │  Compilation │  │  Result     │   │ │
│  │  │  Cache       │  │  Cache       │  │  Cache      │   │ │
│  │  └──────────────┘  └──────────────┘  └─────────────┘   │ │
│  │                                                        │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Component Details

### 1. VS Code Extension Layer

#### 1.1 Extension Entry Point (`extension.ts`)

**Responsibilities:**
- Initialize extension
- Register commands
- Set up event listeners
- Manage extension activation lifecycle

**Key Functions:**
```typescript
export function activate(context: vscode.ExtensionContext) {
  // Initialize
  // Register commands
  // Create UI components
  // Set up workspace watchers
}

export function deactivate() {
  // Clean up
  // Dispose resources
}
```

**Commands to Register:**
- `sharpCodeSearch.search` - Open search interface
- `sharpCodeSearch.replace` - Open search and replace
- `sharpCodeSearch.openCatalog` - Open pattern catalog
- `sharpCodeSearch.createPattern` - Create new pattern
- `sharpCodeSearch.runPattern` - Run saved pattern
- `sharpCodeSearch.editPattern` - Edit saved pattern
- `sharpCodeSearch.deletePattern` - Delete saved pattern

---

#### 1.2 Command Dispatcher

**Responsibilities:**
- Route commands to appropriate handlers
- Handle command parameters
- Manage async operations
- Error handling and user feedback

**Structure:**
```typescript
class CommandDispatcher {
  async handleSearch(pattern: string, scope?: string): Promise<void>
  async handleReplace(pattern: string, replacement: string): Promise<void>
  async handlePatternCrud(operation: 'create'|'read'|'update'|'delete'): Promise<void>
}
```

---

#### 1.3 UI Layer (Webview)

**Components:**
1. **Search Panel**
   - Pattern input with syntax highlighting
   - Search button and options
   - Results tree view
   - Result details panel

2. **Replace Panel**
   - Replace pattern input
   - Preview panel (before/after)
   - Replace options
   - Replace button

3. **Pattern Catalog**
   - Pattern list with search
   - Pattern details view
   - CRUD operations
   - Pattern import/export

4. **Pattern Builder**
   - Visual pattern editor
   - Placeholder palette
   - Constraint builder
   - Live preview

**Communication:**
- VS Code API for document operations
- Message passing for async operations
- Real-time result updates

---

#### 1.4 Result Display Manager

**Responsibilities:**
- Format search results for display
- Navigate to results
- Apply editor decorations (highlighting)
- Manage result grouping and filtering

**Features:**
```typescript
interface SearchResult {
  file: string;
  line: number;
  column: number;
  text: string;
  context: string; // surrounding code
  placeholders: Map<string, string>; // captured values
}

class ResultDisplayManager {
  displayResults(results: SearchResult[]): void
  highlightResult(result: SearchResult): void
  navigateToResult(result: SearchResult): void
  groupResults(results: SearchResult[]): GroupedResults
}
```

---

#### 1.5 Pattern Manager

**Responsibilities:**
- Load/save patterns from storage
- Validate pattern format
- Export/import patterns
- Manage pattern metadata

**Storage Format:**
```json
{
  "patterns": [
    {
      "id": "uuid",
      "name": "Find ToString calls",
      "description": "Finds all ToString() method calls",
      "search": "$obj$.ToString()",
      "replace": "$obj$?.ToString() ?? \"\"",
      "constraints": [],
      "tags": ["refactoring", "null-safety"],
      "createdAt": "2024-02-04",
      "modifiedAt": "2024-02-04"
    }
  ]
}
```

---

### 2. Backend Analysis Engine (.NET)

#### 2.1 Pattern Parser (`PatternParser.cs`)

**Responsibilities:**
- Parse pattern string into AST
- Validate pattern syntax
- Extract placeholders and constraints
- Error reporting

**Implementation:**
```csharp
public class PatternParser
{
    public PatternAst Parse(string pattern) throws PatternParseException
    public IEnumerable<PlaceholderDefinition> ExtractPlaceholders(string pattern)
    public void ValidatePattern(string pattern) throws PatternValidationException
}
```

**Pattern AST Structure:**
```csharp
public abstract class PatternNode { }

public class TextNode : PatternNode
{
    public string Text { get; set; }
}

public class PlaceholderNode : PatternNode
{
    public string Name { get; set; }
    public PlaceholderType Type { get; set; }
    public List<Constraint> Constraints { get; set; }
}

public enum PlaceholderType
{
    Expression,
    Identifier,
    Statement,
    Argument,
    Type
}
```

---

#### 2.2 Pattern Matcher (`PatternMatcher.cs`)

**Responsibilities:**
- Compare pattern AST with code AST
- Extract captured values
- Handle constraint validation
- Track matching state

**Implementation:**
```csharp
public class PatternMatcher
{
    public IEnumerable<Match> FindMatches(
        PatternAst pattern,
        SyntaxNode codeNode,
        SemanticModel semanticModel)

    private bool MatchNode(
        PatternNode patternNode,
        SyntaxNode codeNode,
        Dictionary<string, object> captures)
}

public class Match
{
    public SyntaxNode MatchedNode { get; set; }
    public Dictionary<string, object> Captures { get; set; }
    public TextSpan Span { get; set; }
}
```

**Matching Algorithm (Pseudocode):**
```
Function MatchNode(patternNode, codeNode):
  IF patternNode is TextNode THEN
    RETURN codeNode.ToString() == patternNode.Text
  ELSE IF patternNode is PlaceholderNode THEN
    IF ValidateConstraints(patternNode.Constraints, codeNode) THEN
      captures[patternNode.Name] = codeNode
      RETURN True
    END IF
  ELSE IF patternNode is ExpressionNode THEN
    FOR EACH child IN patternNode.Children DO
      IF NOT MatchNode(child, corresponding_codeNode_child) THEN
        RETURN False
      END IF
    END FOR
    RETURN True
  END IF
  RETURN False
END Function
```

---

#### 2.3 Constraint Validator (`ConstraintValidator.cs`)

**Responsibilities:**
- Validate type constraints
- Apply regex constraints
- Check argument counts
- Validate semantic constraints

**Implementation:**
```csharp
public class ConstraintValidator
{
    public bool ValidateTypeConstraint(
        Constraint constraint,
        SyntaxNode node,
        SemanticModel semanticModel)

    public bool ValidateRegexConstraint(
        string pattern,
        string text)

    public bool ValidateCountConstraint(
        CountConstraint constraint,
        int count)
}
```

**Constraint Types:**
```csharp
public abstract class Constraint { }

public class TypeConstraint : Constraint
{
    public string TypePattern { get; set; } // regex or typename
}

public class RegexConstraint : Constraint
{
    public string Pattern { get; set; }
}

public class CountConstraint : Constraint
{
    public int? MinCount { get; set; }
    public int? MaxCount { get; set; }
}

public class SemanticConstraint : Constraint
{
    public string AccessModifier { get; set; } // public, private, etc.
    public bool? IsAbstract { get; set; }
    public bool? IsStatic { get; set; }
}
```

---

#### 2.4 Workspace Scanner (`WorkspaceScanner.cs`)

**Responsibilities:**
- Find C# files in workspace
- Load project files
- Build compilation context
- Respect gitignore patterns

**Implementation:**
```csharp
public class WorkspaceScanner
{
    public IEnumerable<string> FindCSharpFiles(string workspacePath)
    
    public Compilation BuildCompilation(string workspacePath) throws Exception
    
    public bool IsIgnored(string filePath)
}
```

---

#### 2.5 Replacement Engine (`ReplacementEngine.cs`)

**Responsibilities:**
- Apply replacement patterns
- Format replaced code
- Generate textual changes
- Handle edge cases

**Implementation:**
```csharp
public class ReplacementEngine
{
    public string GenerateReplacement(
        string replacementPattern,
        Match match,
        SemanticModel semanticModel)

    private string SubstitutePlaceholders(
        string template,
        Dictionary<string, object> captures)
}
```

---

#### 2.6 Roslyn Integration Layer (`RoslynHelper.cs`)

**Responsibilities:**
- Simplify Roslyn API usage
- Provide traversal utilities
- Handle common patterns
- Manage semantic information

**Implementation:**
```csharp
public static class RoslynHelper
{
    public static IEnumerable<SyntaxNode> EnumerateNodes(SyntaxNode root)
    
    public static string GetFullName(ISymbol symbol)
    
    public static ITypeSymbol GetExpressionType(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    
    public static bool IsTypeOrDerived(
        ITypeSymbol type,
        ITypeSymbol baseType)
}
```

---

#### 2.7 Analysis Service (`AnalysisService.cs`)

**Orchestrates the analysis process**

**Implementation:**
```csharp
public class AnalysisService
{
    private readonly PatternParser _patternParser;
    private readonly WorkspaceScanner _workspaceScanner;
    private readonly PatternMatcher _patternMatcher;
    private readonly ConstraintValidator _constraintValidator;

    public async Task<IEnumerable<Match>> SearchAsync(
        string pattern,
        string workspacePath,
        IProgress<AnalysisProgress> progress,
        CancellationToken cancellationToken)
    {
        // 1. Parse pattern
        var patternAst = _patternParser.Parse(pattern);
        
        // 2. Scan workspace
        var csharpFiles = _workspaceScanner.FindCSharpFiles(workspacePath);
        
        // 3. Build compilation
        var compilation = _workspaceScanner.BuildCompilation(workspacePath);
        
        // 4. Search each file
        var allMatches = new List<Match>();
        foreach (var file in csharpFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var tree = compilation.SyntaxTrees.First(t => t.FilePath == file);
            var root = tree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(tree);
            
            var matches = _patternMatcher.FindMatches(patternAst, root, semanticModel);
            allMatches.AddRange(matches);
            
            progress.Report(new AnalysisProgress { ProcessedFiles = allMatches.Count });
        }
        
        return allMatches;
    }
}
```

---

### 3. Communication Layer

#### 3.1 Design Decision: CLI vs. LSP

**CLI Approach (Initial MVP):**
- Extension calls backend executable with JSON payload
- Backend returns JSON results
- Simple to implement
- Slower (process startup overhead)
- Suitable for one-off searches

**Language Server Protocol (Future):**
- Persistent backend process
- Faster communication
- Better for interactive use
- More complex to implement
- Industry standard

**Initial Implementation: CLI**
```
Extension → CLI Invocation → Backend Process → JSON Output → Parse & Display
```

**CLI Arguments:**
```bash
SharpCodeSearch.exe search --pattern "$obj$.ToString()" --workspace "C:\Project" --format json
SharpCodeSearch.exe replace --pattern "$pattern$" --replacement "$repl$" --workspace "C:\Project" --format json
```

**JSON Protocol:**

*Request:*
```json
{
  "command": "search",
  "pattern": "$obj$.ToString()",
  "workspace": "C:\\Project",
  "scope": "src/**/*.cs",
  "options": {
    "timeout": 30000,
    "maxResults": 10000
  }
}
```

*Response:*
```json
{
  "success": true,
  "matches": [
    {
      "file": "C:\\Project\\src\\Services\\UserService.cs",
      "line": 45,
      "column": 20,
      "text": "user.ToString()",
      "context": "string name = user.ToString();",
      "placeholders": {
        "obj": "user"
      }
    }
  ],
  "totalMatches": 245,
  "processingTime": 1234
}
```

---

### 4. Caching Layer

#### 4.1 Multi-Level Caching Strategy

**Level 1: Pattern Cache**
```csharp
private static Dictionary<string, PatternAst> _patternCache = new();

public PatternAst GetParsedPattern(string pattern)
{
    if (_patternCache.TryGetValue(pattern, out var cached))
        return cached;
    
    var ast = _patternParser.Parse(pattern);
    _patternCache[pattern] = ast;
    return ast;
}
```

**Level 2: Compilation Cache**
```csharp
private static Dictionary<string, (Compilation, DateTime)> _compilationCache = new();

public Compilation GetCompilation(string workspacePath)
{
    if (_compilationCache.TryGetValue(workspacePath, out var cached))
    {
        if (!IsWorkspaceModified(workspacePath, cached.Item2))
            return cached.Item1;
    }
    
    var compilation = _workspaceScanner.BuildCompilation(workspacePath);
    _compilationCache[workspacePath] = (compilation, DateTime.UtcNow);
    return compilation;
}
```

**Level 3: Result Cache**
```csharp
private static Dictionary<string, (List<Match>, DateTime)> _resultCache = new();

public IEnumerable<Match> GetCachedResults(string cacheKey)
{
    if (_resultCache.TryGetValue(cacheKey, out var cached))
    {
        if ((DateTime.UtcNow - cached.Item2).TotalSeconds < CACHE_TTL)
            return cached.Item1;
    }
    
    return null;
}
```

**Cache Key Strategy:**
```
cache_key = Hash(pattern + workspace_path + files_hash)
```

---

### 5. Data Flow

#### 5.1 Search Flow

```
User enters pattern
    ↓
Extension validates input
    ↓
Extension sends request to backend
    ↓
Backend parses pattern
    ↓
Backend scans workspace
    ↓
Backend builds/loads compilation
    ↓
Backend iterates through SyntaxTrees
    ↓
For each tree:
  - Get SemanticModel
  - Traverse SyntaxNodes
  - Match against pattern
  - Validate constraints
  - Collect matches
    ↓
Backend serializes matches to JSON
    ↓
Extension receives JSON
    ↓
Extension parses results
    ↓
Extension displays results
    ↓
User clicks on result
    ↓
Extension navigates to match location
    ↓
Extension highlights match in editor
```

#### 5.2 Replace Flow

```
User enters replacement pattern
    ↓
Extension requests preview
    ↓
Backend generates preview (same as search)
    ↓
Backend applies replacements (dry run)
    ↓
Backend returns before/after snippets
    ↓
Extension displays preview
    ↓
User confirms replacement
    ↓
Backend applies replacements to files
    ↓
Backend returns modified files
    ↓
Extension writes changes to disk
    ↓
VS Code detects changes
    ↓
Editor reloads modified files
    ↓
User sees changes
```

---

## File Structure

```
SharpCodeSearch/
├── src/
│   ├── extension/
│   │   ├── extension.ts           # Entry point
│   │   ├── commands/
│   │   │   ├── searchCommand.ts
│   │   │   ├── replaceCommand.ts
│   │   │   └── patternCommand.ts
│   │   ├── ui/
│   │   │   ├── searchPanel.ts
│   │   │   ├── replacePanel.ts
│   │   │   ├── patternBuilder.ts
│   │   │   └── resultDisplay.ts
│   │   ├── services/
│   │   │   ├── backendService.ts  # Communication with backend
│   │   │   ├── patternManager.ts
│   │   │   └── resultManager.ts
│   │   └── webview/
│   │       ├── search.html
│   │       ├── search.css
│   │       └── search.js
│   ├── backend/
│   │   ├── Program.cs             # Entry point
│   │   ├── Services/
│   │   │   ├── AnalysisService.cs
│   │   │   ├── PatternParser.cs
│   │   │   ├── PatternMatcher.cs
│   │   │   ├── WorkspaceScanner.cs
│   │   │   ├── ReplacementEngine.cs
│   │   │   └── ConstraintValidator.cs
│   │   ├── Models/
│   │   │   ├── Pattern.cs
│   │   │   ├── Match.cs
│   │   │   ├── Constraint.cs
│   │   │   └── PlaceholderType.cs
│   │   ├── Roslyn/
│   │   │   ├── RoslynHelper.cs
│   │   │   └── CompilationManager.cs
│   │   ├── Caching/
│   │   │   ├── CacheManager.cs
│   │   │   └── CacheStrategy.cs
│   │   └── Protocol/
│   │       ├── Request.cs
│   │       ├── Response.cs
│   │       └── JsonSerializer.cs
├── tests/
│   ├── extension.test.ts
│   ├── backend.test.csproj
│   └── integration.test.ts
├── Docs/
│   ├── RESEARCH.md
│   ├── ROADMAP.md
│   └── ARCHITECTURE.md
├── package.json
├── tsconfig.json
├── SharpCodeSearch.csproj
└── README.md
```

---

## Design Principles

1. **Separation of Concerns**
   - UI logic separate from analysis logic
   - Backend language-agnostic where possible
   - Clear boundaries between components

2. **Performance**
   - Multi-level caching
   - Incremental analysis where possible
   - Progress reporting for long operations

3. **Reliability**
   - Graceful error handling
   - Input validation at all boundaries
   - Comprehensive logging

4. **Extensibility**
   - Plugin pattern for custom constraints
   - Pattern library system
   - Future support for multiple languages

5. **Maintainability**
   - Well-documented code
   - Comprehensive tests
   - Clear API contracts

---

## Future Enhancement: LSP Migration

When transitioning to Language Server Protocol:

```
VS Code Extension Client (TypeScript)
  │
  ├─ Std In/Out
  │
Language Server (C# .NET)
  ├ Analysis Engine
  ├ Pattern Matching
  └ Caching Layer
```

**LSP Methods to Implement:**
- `sharpSearch/search` (custom)
- `sharpSearch/replace` (custom)
- `sharpSearch/preview` (custom)
- `workspace/didChangeConfiguration`
- `textDocument/didOpen`
- `textDocument/didChange`

---

## Error Handling Strategy

**Error Categories:**

1. **Pattern Errors**
   - Invalid syntax
   - Undefined placeholders
   - Type constraint mismatches

2. **Workspace Errors**
   - Project file not found
   - Compilation failures
   - Missing dependencies

3. **Search Errors**
   - Timeout
   - Out of memory
   - Syntax tree corruption

4. **File System Errors**
   - Permission denied
   - File not found
   - Encoding issues

**Error Propagation:**
```
Backend Error → JSON Error Response → Extension Parsing → User Notification
```

---

## Conclusion

This architecture provides a scalable, maintainable foundation for building SharpCodeSearch. The separation of concerns allows independent development of backend analysis and frontend UI. The caching layer ensures reasonable performance even on large codebases. The design is flexible enough to support future enhancements like LSP migration and multi-language support.

# Sharp Code Search - Research Document

## Executive Summary

**SharpCodeSearch** is a VS Code extension project designed to bring semantic code search capabilities to C# codebases, similar to ReSharper's Structural Search and Replace feature. This document provides comprehensive research on the concepts, technologies, and approaches needed to implement this functionality.

---

## 1. What is Semantic Search in C#?

### Definition
Semantic search goes beyond simple text pattern matching to understand the **structural meaning** of code. Instead of searching for exact strings, semantic search uses templated patterns with placeholders to find structurally similar code blocks regardless of:
- Variable names
- Type names
- Method names
- Exact formatting
- Whitespace variations

### Examples of Semantic Search Use Cases

1. **Finding similar method invocations:**
   - Pattern: `$obj$.ToString()`
   - Matches: `user.ToString()`, `config.ToString()`, `item.ToString()`

2. **Detecting code patterns (anti-patterns, refactoring opportunities):**
   - Pattern: `if ($condition$) { return true; } else { return false; }`
   - Matches: Any if-else statement that returns boolean values

3. **API migration:**
   - Pattern: `XmlDocument doc = new XmlDocument();`
   - Can replace with: `var doc = new XDocument();`

4. **Finding dead code or unused patterns:**
   - Pattern: `$var$ = $value$; return;`
   - Matches: Assignment followed immediately by return

---

## 2. ReSharper's Structural Search - Key Features

### 2.1 Placeholder Types
ReSharper supports five types of placeholders for pattern matching:

1. **Argument Placeholder** (`$args$`)
   - Matches one or more arguments in a method invocation
   - Can specify minimal/maximal number of arguments
   - Example: `Console.WriteLine($args$)` matches method calls with any arguments

2. **Expression Placeholder** (`$expr$`)
   - Matches a sequence of operators and operands
   - Can specify return type constraints
   - Example: `x = $expr$;` matches any assignment expression

3. **Identifier Placeholder** (`$id$`)
   - Matches any symbol identifier
   - Can specify regex pattern constraints
   - Example: `public $id$ Method()` matches any public method

4. **Statement Placeholder** (`$stmt$`)
   - Matches single-line or block statements
   - Can specify min/max number of statements
   - Example: `$stmt$; return;` matches any statement before return

5. **Type Placeholder** (`$type$`)
   - Matches any type (value or reference)
   - Can specify exact type constraints
   - Example: `$type$ MyField;` matches any field declaration

### 2.2 Pattern Matching Features
- **Match Similar Constructs**: Matches semantically equivalent code even with minor variations
  - Single statements vs. braced statements
  - Binary expressions in different orders
  - Parenthesized vs. non-parenthesized expressions
  - Prefix vs. postfix operators
  
- **Replace Patterns**: Transform matched code by using placeholders in replacement templates
  - Preserves captured content from search pattern
  - Enables automated refactoring
  - Can apply formatting rules

### 2.3 Pattern Catalog & Storage
- Patterns are stored in a user-accessible catalog
- Patterns can be shared via team configuration
- Each pattern can have:
  - Search description
  - Replace description
  - Severity levels for inspection integration
  - Suppression keys

---

## 3. Technical Architecture for C# Code Analysis

### 3.1 Roslyn - The .NET Compiler Platform

**What is Roslyn?**
- Official C# and VB.NET compiler implementation (.NET Foundation project)
- Exposes rich APIs for code analysis
- Open source (MIT license)
- Provides Abstract Syntax Tree (AST) representation of code

**Why Roslyn for This Project?**
1. **Comprehensive API**: Full access to syntactic and semantic information
2. **Industry Standard**: Used by Visual Studio, ReSharper, and many analyzers
3. **Well-Documented**: Extensive documentation and examples available
4. **Active Development**: Continuously updated with new language features
5. **NuGet Distribution**: Easy integration via NuGet packages

**Key Roslyn Concepts:**
- **SyntaxTree**: Represents the entire source code as a tree structure
- **SyntaxNode**: Individual nodes in the syntax tree (classes, methods, statements, etc.)
- **SyntaxToken**: Leaf nodes (keywords, identifiers, literals)
- **SemanticModel**: Provides semantic information (type information, symbol resolution)
- **Compilation**: Aggregates all syntax trees and metadata for a project

**Roslyn Architecture Overview:**
```
Source Code
    ↓
Lexer (Tokenization)
    ↓
Parser (Syntax Tree Creation)
    ↓
SyntaxTree + References
    ↓
Binder (Semantic Analysis)
    ↓
Compilation (Complete Model)
    ↓
Semantic Analysis & Code Generation
```

### 3.2 Abstract Syntax Tree (AST) Fundamentals

**What is an AST?**
- Tree representation of source code structure
- Captures syntactic structure (not formatting)
- Enables semantic analysis independent of presentation

**Example AST Structure:**
```
MethodInvocation
├── Expression: MemberAccess
│   └── Expression: IdentifierName (obj)
│   └── Name: Identifier (ToString)
└── ArgumentList: []
```

**Why ASTs for Semantic Search:**
- Enables pattern matching at structural level
- Ignores whitespace and formatting variations
- Allows type-aware matching through semantic model
- Foundation for all code analysis tools

### 3.3 Pattern Matching Algorithm

**High-Level Approach:**
1. **Parse Pattern**: Convert search pattern string into Roslyn AST
2. **Traverse Target**: Walk through target code AST
3. **Match Nodes**: Compare current node with pattern using matching rules
4. **Placeholder Resolution**: Extract values for placeholders
5. **Constraint Validation**: Verify additional constraints (types, regex, min/max)
6. **Report Results**: Collect all matches with location information

**Matching Challenges:**
- Handling placeholder variations (single vs. multiple)
- Type constraint satisfaction
- Semantic equivalence (e.g., `x++` vs. `++x`)
- Reference resolution (what does this identifier refer to?)

---

## 4. VS Code Extension Architecture

### 4.1 Extension Basics

**Core Components:**
1. **Extension Manifest** (`package.json`)
   - Declares plugin metadata
   - Lists activation events
   - Defines contribution points (commands, views, etc.)

2. **Extension Entry Point** (usually `extension.js` or `extension.ts`)
   - Loaded when activation event occurs
   - Provides `activate()` function
   - Can provide `deactivate()` cleanup function

3. **VS Code API Access**
   - Access to editor, workspace, commands, etc.
   - Event subscriptions (document changes, selections, etc.)
   - UI components (views, webviews, status bar, etc.)

### 4.2 VS Code Extension Capabilities Relevant to SharpCodeSearch

**Editor Commands & Context**
- Register commands for search, search-replace
- Access document content and selections
- Modify editor state and decorations

**Document Analysis**
- Hook into document open/change events
- Get document URI, language, content
- Apply code highlighting and diagnostics

**Search & Navigation**
- Display search results in dedicated panel
- Navigate to search results with location info
- Preview code context

**UI Integration**
- Custom webview for pattern editor/builder
- Sidebar view for search interface
- Status bar updates

**Configuration**
- User settings for pattern catalog
- Extension configuration storage
- Workspace-level pattern sharing

### 4.3 Architecture Pattern: Language Server Protocol (LSP)

**Why Consider LSP?**
- Separation of language-specific logic from UI
- Can run analysis in separate process (better performance)
- Language agnostic - reusable backend
- Future: Can support multiple editors (VS Code, Neovim, etc.)

**LSP Components:**
- **Language Client**: VS Code extension
- **Language Server**: Separate process (C# project)
- **Communication**: JSON-RPC over stdio, pipes, or network

**LSP Methods for Semantic Search:**
- Custom methods for pattern search
- Custom methods for pattern validation
- Use standard `textDocument/hover` for displaying results

---

## 5. Technology Stack Recommendations

### Backend (Analysis Engine)
- **Language**: C# (native support for Roslyn)
- **Framework**: .NET 8+ (cross-platform)
- **Core Dependencies**:
  - `Microsoft.CodeAnalysis` (Roslyn)
  - `Microsoft.CodeAnalysis.CSharp`
  - `Microsoft.CodeAnalysis.Workspaces`

### Frontend (VS Code Extension)
- **Language**: TypeScript
- **Runtime**: Node.js (built into VS Code)
- **Dependencies**:
  - `vscode` API
  - `vscode-languageclient` (if using LSP)

### Communication
- **Option 1 (Simple)**: CLI invocation from extension
  - Extension calls .NET executable with pattern
  - Executable returns JSON results
  - Simple but slower

- **Option 2 (Recommended)**: Language Server Protocol
  - Persistent backend connection
  - Better performance for interactive use
  - More complex to implement

---

## 6. Pattern Query Language Design

### 6.1 Pattern Syntax

**Proposed Syntax** (inspired by ReSharper):
```
// Find all method calls on any object
$obj$.ToString()

// Find field initialization
public $type$ $field$ = $value$;

// Find foreach loops iterating over IEnumerable<T>
foreach ($type$ $var$ in $collection$)
{
    $body$
}

// Find assignments followed by return
$var$ = $expr$;
return;

// Find specific pattern with constraints
public $type:[System\\.Collections\\..+]$ $method$()
{
    return new $type$();
}
```

### 6.2 Constraint System

**Type Constraints:**
- Exact type: `$type:string$`
- Namespace pattern: `$type:System\..*$`
- Base type: `$type:IEnumerable$` (matches derived types)

**Identifier Constraints:**
- Regex pattern: `$id:[A-Z][a-zA-Z0-9]*$` (Pascal case)
- Prefix match: `$id:^Get.*$`

**Count Constraints:**
- Minimum: `$statements:2..∞$`
- Maximum: `$statements:0..5$`
- Exact: `$args:3$`

---

## 7. Development Phases

### Phase 1: Foundation & Prototyping
- Set up VS Code extension project structure
- Implement basic Roslyn integration
- Create simple pattern parser
- Build CLI tool for pattern matching

### Phase 2: Core Functionality
- Implement semantic search algorithm
- Add support for all placeholder types
- Build search result collection and reporting
- Create extension UI for search interface

### Phase 3: Advanced Features
- Add pattern replacement capability
- Implement pattern catalog system
- Add constraint validation
- Build pattern builder/editor UI

### Phase 4: Optimization & Polish
- Performance optimization
- Caching mechanisms
- Error handling and UX improvements
- Documentation and examples

### Phase 5: Distribution & Ecosystem
- Package as VS Code extension
- Publish to marketplace
- Create pattern library
- Community engagement

---

## 8. Challenges & Considerations

### 8.1 Technical Challenges

**Semantic Analysis Complexity**
- Type resolution requires full semantic model
- Async/await patterns complicate matching
- Nullable reference types need careful handling
- Generic type constraints require sophisticated matching

**Performance**
- Analyzing large codebases is computationally expensive
- Need efficient pattern matching algorithm
- Caching and incremental analysis are critical
- Language server approach essential for good UX

**Pattern Language Design**
- Must be powerful yet intuitive
- Need clear documentation and examples
- Balance between simplicity and expressiveness
- Escaping and special character handling

### 8.2 UX Considerations

**User Education**
- Users unfamiliar with ReSharper may need learning curve
- Pattern syntax must be learnable
- Good documentation and examples essential
- Pattern builder UI can help

**Result Navigation**
- Results must be easy to navigate and review
- Context display important for understanding matches
- Preview functionality essential
- Integration with VS Code's find/replace workflow

**Performance Feedback**
- Long-running searches need progress indication
- Cancellation support important
- Results should update incrementally

---

## 9. Related Tools & References

### Similar Tools
1. **ReSharper** (Commercial, JetBrains)
   - Gold standard for C# structural search
   - Commercial product with full IDE integration

2. **Visual Studio Search** (Microsoft)
   - Basic semantic search for symbols
   - Integrated into Visual Studio 2022+
   - More limited than ReSharper

3. **NetPad** (Open Source)
   - C# code runner with Roslyn integration
   - Good reference for Roslyn usage

4. **CSharpier** (Open Source)
   - Roslyn-based code formatter
   - Good example of AST traversal

### Key Resources

**Roslyn Documentation:**
- https://docs.microsoft.com/dotnet/csharp/roslyn-sdk/
- GitHub: https://github.com/dotnet/roslyn

**VS Code Extension Development:**
- https://code.visualstudio.com/api
- https://github.com/microsoft/vscode-extension-samples

**Language Server Protocol:**
- https://microsoft.github.io/language-server-protocol/
- https://github.com/microsoft/language-server-protocol

**ReSharper Structural Search:**
- https://www.jetbrains.com/help/resharper/Navigation_and_Search__Structural_Search_and_Replace.html

---

## 10. Success Criteria

### MVP (Minimum Viable Product)
- [ ] Can match basic method invocation patterns
- [ ] Can find matches in single file
- [ ] Can display results in VS Code
- [ ] Supports simple placeholder types (Expression, Identifier)
- [ ] Pattern validation and error reporting

### Version 1.0
- [ ] Search entire workspace
- [ ] All placeholder types supported
- [ ] Basic constraint system
- [ ] Search and replace functionality
- [ ] Pattern catalog system
- [ ] Marketplace release

### Future Enhancements
- [ ] AI-assisted pattern generation
- [ ] Collaborative pattern sharing
- [ ] Performance optimizations (incremental analysis)
- [ ] Integration with code analysis/linting tools
- [ ] Pattern templates for common refactorings

---

## Conclusion

Creating a semantic code search tool for C# in VS Code is an ambitious but achievable project. The combination of mature technologies (Roslyn, VS Code API) and clear reference implementations (ReSharper) provides a solid foundation. The main challenges lie in performance optimization, pattern language design clarity, and user experience. Following a phased approach will allow for incremental value delivery and community feedback.

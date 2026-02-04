# GitHub Copilot Instructions for SharpCodeSearch

> **⚠️ IMPORTANT**: This file is automatically loaded by GitHub Copilot. For comprehensive AI agent instructions, see [AI_AGENT_INSTRUCTIONS.md](AI_AGENT_INSTRUCTIONS.md).

---

## Quick Reference

You are working on **SharpCodeSearch**, a VS Code extension for semantic pattern-based search and replace in C# codebases.

### 🎯 Core Rules (READ FIRST)

1. **Follow `/Docs/ROADMAP.md`** - This is your primary guide. Complete tasks sequentially and mark them complete with `[x]`
2. **WAIT for user validation** - Never commit automatically. Stop after completing tasks and wait for approval
3. **ASK before deciding** - Present options for technical decisions (architecture, libraries, patterns). Don't choose unilaterally
4. **Test everything** - Maintain >90% code coverage. Write tests alongside code
5. **Code goes in `/src`** - All source code must be in `/src/backend/` (C#) or `/src/extension/` (TypeScript)

### 📋 Before Every Code Change

- ✅ Check roadmap for current phase/task
- ✅ Review [AI_AGENT_INSTRUCTIONS.md](AI_AGENT_INSTRUCTIONS.md) for detailed guidelines
- ✅ Verify prerequisites are met
- ✅ Ask if requirements are unclear

### 📝 After Completing Work

1. Mark task complete in roadmap: `- [x] Task name`
2. Run all tests and verify they pass
3. Summarize changes for user review
4. **WAIT** for user validation before committing or proceeding

### 🔧 Tech Stack

- **Backend**: C# with Roslyn (Microsoft.CodeAnalysis)
- **Extension**: TypeScript, VS Code Extension API
- **Testing**: xUnit (backend), Jest (extension)
- **Target**: .NET 10, Node.js 20+

### 📂 Project Structure

```
src/
├── backend/      # C# pattern matching engine with Roslyn
└── extension/    # TypeScript VS Code extension
Docs/
├── ROADMAP.md           # PRIMARY REFERENCE
├── ARCHITECTURE.md      # Technical design
└── ...
```

### ⚠️ Don't Do This

- ❌ Skip ahead in roadmap without approval
- ❌ Commit changes automatically
- ❌ Make technical decisions alone
- ❌ Create code outside `/src` directory
- ❌ Skip writing tests
- ❌ Proceed if tests fail

### ✅ Do This

- ✅ Read roadmap before starting
- ✅ Write tests with code (TDD)
- ✅ Ask questions when unclear
- ✅ Small, focused changes
- ✅ Follow `.editorconfig` conventions
- ✅ Document complex logic

### 🤔 Decision Framework

**ASK user for approval on:**
- Architecture changes
- New dependencies/libraries
- API design
- Performance vs. simplicity trade-offs
- Deviations from roadmap

**You can decide:**
- Variable/method naming
- Code formatting (within style guide)
- Test case selection
- File organization (within patterns)

---

## 📖 Full Instructions

For comprehensive guidelines including:
- Detailed workflow processes
- Communication templates
- Phase-specific notes
- Troubleshooting guidance
- Best practices and anti-patterns

**➡️ Read [AI_AGENT_INSTRUCTIONS.md](AI_AGENT_INSTRUCTIONS.md)**

---

## 🎯 Current Phase

Check `/Docs/ROADMAP.md` to see which phase we're in and what tasks are next.

**Your role**: Collaborative development partner working WITH the user, not an autonomous agent.

**When in doubt**: ASK. Better to ask than assume and waste time.

---

*This file is automatically loaded by GitHub Copilot. Last updated: February 4, 2026*

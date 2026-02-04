# Phase 1.4 Testing Guide

## Quick Start Testing

### 1. Launch Extension Development Host

1. Open the SharpCodeSearch workspace in VS Code
2. Press **F5** to start debugging
3. A new "Extension Development Host" window will open

### 2. Open a Test Project

In the Extension Development Host window:
- Open a folder containing C# files (.cs)
- Or use the `src/backend/` folder itself as a test workspace

### 3. Test the Search Command

#### Via Command Palette:
1. Press **Ctrl+Shift+P** (Cmd+Shift+P on Mac)
2. Type "Sharp Code Search: Search"
3. Press Enter

#### Expected Result:
- A webview panel titled "Sharp Code Search" should open
- You should see:
  - Pattern input textarea
  - Match case/Whole word checkboxes
  - Search and Clear buttons
  - Empty results section with "Enter a pattern and click Search"

### 4. Test Pattern Input

Try entering these patterns:
- `$obj$.ToString()` - Find all ToString() calls
- `$method$($args$)` - Find all method calls
- `$expr$` - Find all expressions
- `Console.WriteLine` - Literal text search

#### Expected Behavior:
- Pattern validation on Search click
- Error message if pattern has unbalanced $ symbols
- Clear button should empty the input and reset results

### 5. Test Search Execution

1. Enter pattern: `$obj$.ToString()`
2. Click **Search** button

#### Current Expected Behavior (Phase 1.4):
- Status message: "Searching..."
- Search button becomes disabled
- Backend CLI is executed

#### Known Limitation:
Since the backend doesn't output JSON yet, you'll see:
- Either an error message (if backend output isn't JSON)
- Or empty results (if backend output is empty)
- Check the Debug Console for backend stderr/stdout

This is **expected** - the UI is complete, but backend JSON output is pending.

### 6. Test UI Features

#### Pattern Input:
- ✅ Multi-line textarea
- ✅ Placeholder text visible
- ✅ Help text displays
- ✅ Ctrl+Enter to search (in textarea)

#### Options:
- ✅ Match case checkbox (checked by default)
- ✅ Whole word checkbox

#### Results Section:
- ✅ Shows "0 matches" initially
- ✅ Empty state with search icon
- ✅ Scrollable container

#### Buttons:
- ✅ Search button has icon
- ✅ Disabled during search
- ✅ Clear button resets form

### 7. Test Theme Support

Switch VS Code themes to verify styling:
1. **Ctrl+K Ctrl+T** to open theme picker
2. Try:
   - Dark+ (default dark)
   - Light+ (default light)
   - Any other theme

#### Expected:
- Colors adapt to theme
- Input fields use theme colors
- Buttons use VS Code button colors
- Text remains readable

### 8. Check Console Logs

Open Debug Console in the main VS Code window:
- **View** → **Debug Console**
- Look for:
  - "Sharp Code Search extension is now active"
  - "Sharp Code Search: All commands registered successfully"
  - Warning if backend not found (expected in dev)

### 9. Test Error Handling

Try these error scenarios:

#### Empty Pattern:
1. Leave pattern empty
2. Click Search
3. Should show: "Please enter a search pattern" error

#### Invalid Pattern:
1. Enter: `$incomplete`
2. Click Search
3. Should show: "Invalid pattern syntax" error

#### No Workspace:
1. Close all folders (File → Close Folder)
2. Open Command Palette
3. Commands should still appear
4. Running search should show workspace error

## Backend Integration Testing (When Ready)

Once the backend outputs JSON, test:

### Expected JSON Format:
```json
[
  {
    "file": "path/to/file.cs",
    "line": 10,
    "column": 5,
    "code": "obj.ToString()",
    "matchedText": "obj.ToString()",
    "placeholders": {
      "obj": "myObject"
    }
  }
]
```

### Test Result Display:
1. Results should appear in list
2. Each result shows:
   - File path (clickable)
   - Line and column
   - Code preview
3. Results count updates: "X matches"

### Test Result Navigation:
1. Click on a result item
2. Should:
   - Highlight the selected result
   - Open the file in editor
   - Jump to the line/column
   - Show details panel

### Test Details Panel:
1. Click a result
2. Details panel appears on right
3. Shows:
   - File path
   - Location (line/column)
   - Matched code
   - Placeholder values (if any)
4. Close button (×) hides panel

## Checklist: Phase 1.4 Success Criteria

- [ ] Search panel opens in sidebar/editor ✅
- [ ] Pattern input works ✅
- [ ] Search button executes backend ✅
- [ ] Clear button resets form ✅
- [ ] Commands appear in palette ✅
- [ ] Theme support working ✅
- [ ] Error messages display ✅
- [ ] Loading state shows ✅

### Pending (Requires Backend JSON Output):
- [ ] Results display in list
- [ ] Can navigate to results
- [ ] Details panel shows match info
- [ ] Result highlighting works

## Troubleshooting

### Extension doesn't activate:
- Check Debug Console for errors
- Verify `out/` folder exists: `src/extension/out/`
- Run: `npm run compile` in `src/extension/`

### "Backend not found" warning:
- Expected in development
- Backend path: `src/backend/bin/Debug/net10.0/SharpCodeSearch.exe`
- Build backend: `dotnet build` in `src/backend/`

### Webview doesn't open:
- Check for console errors
- Verify files exist:
  - `src/extension/webview/search.html`
  - `src/extension/webview/search.css`
  - `src/extension/webview/search.js`

### Search doesn't work:
- Open Debug Console
- Look for backend execution errors
- Verify workspace folder is open
- Check backend is built

### Styling looks wrong:
- Hard refresh webview: Close panel and reopen
- Check browser dev tools: Ctrl+Shift+I (when webview focused)
- Verify CSS file loaded (check Network tab)

## Next Steps

After Phase 1.4 testing:

1. **Enhance Backend CLI** (Phase 1.5):
   - Add JSON output format
   - Implement actual pattern matching
   - Return proper result objects

2. **Integration Testing**:
   - Test full search workflow
   - Verify result navigation
   - Test on real C# projects

3. **Document Findings**:
   - Note any bugs
   - Performance observations
   - UX improvements needed

---

**Phase 1.4 Status:** ✅ Implementation Complete, Ready for Testing

**Next Phase:** 1.5 Integration Testing & Completion

# Contributing to SharpCodeSearch

Thank you for your interest in contributing to SharpCodeSearch! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and collaborative environment for all contributors.

## How to Contribute

### Reporting Issues

- Search existing issues before creating a new one
- Provide a clear description of the problem
- Include steps to reproduce the issue
- Specify your environment (OS, .NET version, VS Code version)
- Include relevant code samples or patterns if applicable

### Suggesting Features

- Check if the feature has already been requested
- Clearly describe the feature and its use case
- Explain how it would benefit users
- Be open to discussion and feedback

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the code style** - use the included `.editorconfig`
3. **Write tests** for new functionality
4. **Update documentation** as needed
5. **Keep commits focused** - one logical change per commit
6. **Write clear commit messages** following conventional commits format

#### Commit Message Format

```
type(scope): brief description

Longer description if needed

Fixes #issue-number
```

Types: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `chore`

Example:
```
feat(matcher): add support for statement placeholders

Implements statement placeholder matching with min/max count constraints.

Fixes #42
```

### Development Setup

1. Install prerequisites:
   - .NET 10 SDK
   - Node.js 20+ (LTS)
   - VS Code

2. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/SharpCodeSearch.git
   cd SharpCodeSearch
   ```

3. Set up the backend:
   ```bash
   cd src/backend
   dotnet restore
   dotnet build
   ```

4. Set up the extension:
   ```bash
   cd src/extension
   npm install
   npm run compile
   ```

5. Run tests:
   ```bash
   # Backend tests
   cd src/backend
   dotnet test

   # Extension tests
   cd src/extension
   npm test
   ```

### Code Style

- **C# Backend**: Follow standard C# conventions, use `editorconfig` settings
- **TypeScript Extension**: Follow VS Code extension best practices
- **Formatting**: Code will be checked by linters/formatters in CI
- **Naming**: Use descriptive names, avoid abbreviations unless common

### Testing Guidelines

- Write unit tests for all new functionality
- Maintain or improve code coverage (target >90%)
- Test edge cases and error conditions
- Add integration tests for end-to-end workflows
- Include performance tests for matching algorithms

### Documentation

- Update README.md if adding user-facing features
- Document public APIs with XML comments (C#) or JSDoc (TypeScript)
- Add examples for new pattern types
- Update ROADMAP.md if affecting project timeline

### Review Process

1. Submit pull request with clear description
2. Address review feedback promptly
3. Keep the PR focused and reasonably sized
4. Be patient - maintainers will review as time permits
5. Once approved, a maintainer will merge your PR

## Project Structure

```
SharpCodeSearch/
├── src/
│   ├── backend/          # C# pattern matching engine
│   └── extension/        # VS Code extension
├── Docs/                 # Project documentation
└── tests/                # Test projects
```

## Getting Help

- Review existing documentation in `/Docs`
- Search closed issues for similar questions
- Open a discussion for general questions
- Join community discussions (when available)

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

## Recognition

Contributors will be recognized in release notes and the project README. Significant contributions may result in commit access to the repository.

---

Thank you for helping make SharpCodeSearch better!

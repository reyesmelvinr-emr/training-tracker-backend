# AKR Vale Rules

Vale prose linter configuration for enforcing AKR documentation standards.

## Overview

This directory contains Vale rules that automatically check AKR documentation for:
- **Required sections** (organization-mandatory)
- **Transparency markers** (🤖 ❓ 👤)
- **Terminology consistency** (service naming, technical terms)
- **Writing style** (active voice, sentence case, clarity)
- **Content quality** (minimum length, code examples)

## Installation

### 1. Install Vale

```bash
# macOS
brew install vale

# Windows (Scoop)
scoop install vale

# Windows (Chocolatey)
choco install vale

# Linux
snap install vale

# Or download from: https://github.com/errata-ai/vale/releases
```

### 2. Copy Configuration

The `.vale.ini` file should be in your repository root or `~/.akr/templates/` directory.

## Usage

### Command Line

```bash
# Check a single file
vale docs/services/UserService.md

# Check all markdown files
vale docs/**/*.md

# Check with specific config
vale --config=.akr/.vale.ini docs/
```

### VS Code Integration

Install the Vale extension:
- Extension: `errata-ai.vale-server`
- Provides real-time linting in VS Code

### GitHub Actions

```yaml
- name: Lint documentation with Vale
  uses: errata-ai/vale-action@v2
  with:
    files: docs
    version: 2.29.0
  env:
    GITHUB_TOKEN: ${{secrets.GITHUB_TOKEN}}
```

## Rule Severity Levels

### Error (Blocks Merge)
- **TransparencyMarkers.yml** - Must include 🤖 ❓ or 👤 markers
- **RequiredSections.yml** - Must include mandatory sections (Overview, Purpose, Dependencies, Key methods)

### Warning (Requires Review)
- **HeadingCase.yml** - Headings should use sentence case
- **MinimumLength.yml** - Sections should have sufficient content (50+ chars)
- **FutureTense.yml** - Avoid documenting future plans
- **Terminology.yml** - Use consistent technical terms

### Suggestion (Non-blocking)
- **PassiveVoice.yml** - Prefer active voice
- **CodeExamples.yml** - Include code examples where helpful

## Rule Descriptions

### TransparencyMarkers.yml
Ensures documentation includes transparency markers to indicate content source:
- 🤖 AI-generated content
- ❓ Human input needed
- 👤 Human-authored content

**Level:** Error (organization-mandatory)

### RequiredSections.yml
Validates presence of mandatory sections per AKR Charter:
- Overview
- Purpose and scope
- Dependencies
- Key methods

**Level:** Error (organization-mandatory)

### HeadingCase.yml
Enforces sentence case for headings (not Title Case).
- ✅ "How to use this service"
- ❌ "How To Use This Service"

**Level:** Warning

### Terminology.yml
Enforces consistent technical terminology:
- `EnrollmentService` not `ServiceEnrollment`
- `API` not `Web API` or `web api`
- `RESTful` not `restful` or `Restful`
- `JSON` not `json` or `Json`

**Level:** Warning

### PassiveVoice.yml
Suggests active voice for clarity:
- ✅ "The service validates the input"
- ⚠️ "The input is validated by the service"

**Level:** Suggestion

### FutureTense.yml
Prevents documenting future plans:
- ❌ "will be implemented"
- ❌ "coming soon"
- ❌ "future version"

**Level:** Warning

### MinimumLength.yml
Ensures sections have sufficient content (not just placeholders).

**Level:** Warning

### CodeExamples.yml
Suggests adding code examples using fenced code blocks.

**Level:** Suggestion

## Vocabulary Files

### accept.txt
Terms that Vale will not flag:
- AKR-specific terms
- Technology names (API, REST, JSON, etc.)
- Framework names (ASP.NET Core, Node.js, etc.)
- Architecture patterns (microservices, CRUD, etc.)

### reject.txt
Terms that Vale will always flag:
- Informal language (gonna, wanna, kinda)
- Vague terms (somehow, something, someone)
- Weak modifiers (very, really, quite)
- Future tense phrases (will be implemented, coming soon)

## Customization

### Add Custom Rules

Create a new `.yml` file in `AKR/` directory:

```yaml
---
# Rule: YourRuleName
extends: existence
message: "Your custom message"
level: warning  # error, warning, or suggestion
scope: text
tokens:
  - 'pattern to match'
```

### Modify Severity Levels

Edit the `level:` field in any rule file:
- `error` - Blocks merge (organization-mandatory)
- `warning` - Requires review
- `suggestion` - Optional guidance

### Add Accepted Terms

Add to `accept.txt`:
```
YourCustomTerm
AnotherAcceptedTerm
```

### Add Rejected Terms

Add to `reject.txt`:
```
badterm
anotherBadTerm
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Documentation Validation

on: [pull_request]

jobs:
  vale:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Vale Linter
        uses: errata-ai/vale-action@v2
        with:
          files: docs
          fail_on_error: true
          version: 2.29.0
        env:
          GITHUB_TOKEN: ${{secrets.GITHUB_TOKEN}}
```

### Branch Protection

Configure branch protection to require Vale checks to pass:
1. Repository Settings → Branches
2. Add rule for `main` branch
3. Require status checks: `Vale`
4. Critical failures block merge

## Testing Rules

```bash
# Test all rules
vale --config=.akr/.vale.ini docs/

# Test specific rule
vale --config=.akr/.vale.ini --glob='*.yml' docs/

# Output JSON for parsing
vale --output=JSON docs/ > vale-results.json
```

## Troubleshooting

### Vale Not Finding Config

```bash
# Specify config explicitly
vale --config=.akr/.vale.ini docs/

# Or set environment variable
export VALE_CONFIG_PATH=.akr/.vale.ini
```

### False Positives

Add exceptions to `accept.txt` or use ignore comments:

```markdown
<!-- vale off -->
This text will not be checked by Vale.
<!-- vale on -->

<!-- vale AKR.Terminology = NO -->
This ignores only the Terminology rule.
<!-- vale AKR.Terminology = YES -->
```

### Rule Not Triggering

Check:
1. Rule file syntax (YAML)
2. Scope matches content
3. Tokens/patterns match text
4. Rule level in `.vale.ini`

## Resources

- [Vale Documentation](https://vale.sh/docs/)
- [Vale Styles Gallery](https://vale.sh/hub/)
- [AKR Charter](../charters/AKR_CHARTER_BACKEND.md)
- [Writing Style Guide](https://developers.google.com/style)

---

**Version:** 1.0  
**Last Updated:** January 14, 2026  
**Maintained by:** AKR Development Team

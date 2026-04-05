#!/usr/bin/env python3
"""
AKR Inline Validator — lightweight post-generation check.

Runs immediately after document generation in the developer's local environment.
Requires NO external files, NO pip install beyond stdlib, and NO distribution of
validate_documentation.py to application repos.

CONSTANTS_VERSION: 1.0.0
  This version string must match the CONSTANTS_VERSION in validate_documentation.py.
  When shared enums (VALID_LAYERS, VALID_PROJECT_TYPES, VALID_STATUSES,
  VALID_COMPLIANCE_MODES) change in either file, bump this version in both.
  A CI test asserts that both files declare the same CONSTANTS_VERSION and
  that all shared enum sets are identical.

Checks (this file):
  - YAML front matter presence and required field completeness
  - Field value validity (layer, project_type, status, compliance_mode)
  - Draft-only front matter fields absent from final output
  - akr-generated metadata header presence
  - Required section headings (read from akr:section directives in the file,
    falls back to BASELINE_REQUIRED_SECTIONS for pre-directive documents)
  - Unresolved ❓ markers (warning in pilot, error in production)
  - DEFERRED markers (warning — verify owner + follow-up)

Deferred to CI (validate_documentation.py):
  - modules.yaml schema validity and cross-field checks
  - doc_output path registration and duplicate detection
  - declared-artifacts warnings (draft/review file existence)
  - Vale prose linting
  - Completeness scoring with penalty model
  - Cross-module relationship checks
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple

# ---------------------------------------------------------------------------
# SHARED CONSTANTS — keep in sync with validate_documentation.py
# When any value below changes, bump CONSTANTS_VERSION in BOTH files.
# A CI test (tests/test_constants_sync.py) asserts equality across both files.
# ---------------------------------------------------------------------------
CONSTANTS_VERSION = "1.0.0"

VALID_LAYERS = frozenset({
    "UI", "API", "Database", "Integration", "Infrastructure", "Full-Stack"
})
VALID_PROJECT_TYPES = frozenset({
    "api-backend", "ui-component", "microservice", "general"
})
VALID_STATUSES = frozenset({
    "draft", "review", "approved", "in-progress", "deprecated"
})
VALID_COMPLIANCE_MODES = frozenset({"pilot", "production"})

# Required front matter field names (lowercase for case-insensitive comparison)
REQUIRED_FRONT_MATTER_FIELDS = frozenset({
    "businesscapability",
    "feature",
    "layer",
    "project_type",
    "status",
    "compliance_mode",
})

# Draft-only fields that must not appear in final output
DRAFT_ONLY_FIELDS = frozenset({"preview-generated-at", "review-mode"})

# ---------------------------------------------------------------------------
# BASELINE required sections — used when no akr:section directives are found.
# Covers documents generated before the directive pattern was introduced.
# Keep in sync with MODULE_REQUIRED_SECTIONS in validate_documentation.py.
# ---------------------------------------------------------------------------
BASELINE_REQUIRED_SECTIONS = [
    "Quick Reference (TL;DR)",
    "Module Files",
    "Operations Map",
    "Architecture Overview",
    "Business Rules",
    "Data Operations",
    "Questions & Gaps",
]


# ---------------------------------------------------------------------------
# HTML comment extractor (re.DOTALL — handles multi-line directives correctly)
# Replaces the fragile [^>]+? pattern from the original inline validator.
# ---------------------------------------------------------------------------
_COMMENT_RE = re.compile(r"<!--(.*?)-->", re.DOTALL)
_SECTION_DIRECTIVE_REQUIRED_RE = re.compile(r"\brequired\s*=\s*(true|false)\b", re.IGNORECASE)
_SECTION_DIRECTIVE_CONDITION_RE = re.compile(r"\bcondition\s*=\s*(\S+)")
_SECTION_DIRECTIVE_ID_RE = re.compile(r"\bid\s*=\s*(\S+)")


def _extract_required_sections_from_directives(content: str) -> Optional[List[str]]:
    """
    Parse akr:section directives from the document content.

    Returns a list of section ids that are required=true and have no condition,
    or None if no akr:section directives are found (triggers baseline fallback).
    """
    found_any = False
    required_ids: List[str] = []

    for match in _COMMENT_RE.finditer(content):
        body = match.group(1)
        stripped = body.strip()

        # Only process akr:section blocks
        first_token = stripped.split()[0] if stripped.split() else ""
        if first_token != "akr:section":
            continue

        found_any = True

        id_m = _SECTION_DIRECTIVE_ID_RE.search(stripped)
        required_m = _SECTION_DIRECTIVE_REQUIRED_RE.search(stripped)
        condition_m = _SECTION_DIRECTIVE_CONDITION_RE.search(stripped)

        if not id_m:
            continue

        section_id = id_m.group(1)
        is_required = required_m and required_m.group(1).lower() == "true"
        has_condition = bool(condition_m)

        if is_required and not has_condition:
            required_ids.append(section_id)

    return required_ids if found_any else None


def _section_id_to_heading(section_id: str) -> str:
    """Convert snake_case section id to Title Case heading for display."""
    return section_id.replace("_", " ").title()


def _normalize_heading(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", text.lower())


def _extract_h2_headings(content: str) -> Dict[str, int]:
    """Return normalized heading → line number for all ## headings."""
    headings: Dict[str, int] = {}
    for idx, line in enumerate(content.splitlines(), start=1):
        if line.startswith("## "):
            norm = _normalize_heading(line[3:].strip())
            headings[norm] = idx
    return headings


# ---------------------------------------------------------------------------
# Front matter parser
# ---------------------------------------------------------------------------

def _parse_front_matter(content: str) -> Tuple[Dict[str, str], bool]:
    lines = content.splitlines()
    if not lines or lines[0].strip() != "---":
        return {}, False

    fields: Dict[str, str] = {}
    for line in lines[1:]:
        if line.strip() == "---":
            break
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        fields[key.strip().lower()] = value.strip()
    return fields, True


# ---------------------------------------------------------------------------
# Individual checks
# ---------------------------------------------------------------------------

def _check_front_matter(
    content: str,
    front_matter: Dict[str, str],
    found: bool,
    is_final: bool,
) -> List[Dict]:
    issues = []

    if not found:
        issues.append({
            "severity": "error",
            "rule": "front-matter",
            "message": "YAML front matter block not found (document must start with ---)",
            "line": 1,
        })
        return issues

    for field in sorted(REQUIRED_FRONT_MATTER_FIELDS):
        if not front_matter.get(field, "").strip():
            issues.append({
                "severity": "error",
                "rule": "front-matter",
                "message": f"Missing required front matter field: {field}",
                "line": None,
            })

    layer = front_matter.get("layer", "")
    if layer and layer not in VALID_LAYERS:
        issues.append({
            "severity": "error",
            "rule": "front-matter",
            "message": (
                f"Invalid layer: '{layer}'. "
                f"Must be one of: {', '.join(sorted(VALID_LAYERS))}"
            ),
            "line": None,
        })

    project_type = front_matter.get("project_type", "")
    if project_type and project_type not in VALID_PROJECT_TYPES:
        issues.append({
            "severity": "error",
            "rule": "front-matter",
            "message": (
                f"Invalid project_type: '{project_type}'. "
                f"Must be one of: {', '.join(sorted(VALID_PROJECT_TYPES))}"
            ),
            "line": None,
        })

    status = front_matter.get("status", "")
    if status and status not in VALID_STATUSES:
        issues.append({
            "severity": "error",
            "rule": "front-matter",
            "message": (
                f"Invalid status: '{status}'. "
                f"Must be one of: {', '.join(sorted(VALID_STATUSES))}"
            ),
            "line": None,
        })

    compliance = front_matter.get("compliance_mode", "")
    if compliance and compliance not in VALID_COMPLIANCE_MODES:
        issues.append({
            "severity": "error",
            "rule": "front-matter",
            "message": f"Invalid compliance_mode: '{compliance}'. Must be pilot or production.",
            "line": None,
        })

    if is_final:
        for draft_field in DRAFT_ONLY_FIELDS:
            if draft_field in front_matter:
                issues.append({
                    "severity": "error",
                    "rule": "final-doc-cleanliness",
                    "message": (
                        f"Draft-only field '{draft_field}' present in final output. "
                        "Strip all draft-only front matter before writing final document."
                    ),
                    "line": None,
                })

    return issues


def _check_metadata_header(content: str) -> List[Dict]:
    if re.search(r"<!--\s*akr-generated\b", content):
        return []
    return [{
        "severity": "error",
        "rule": "akr-generated-header",
        "message": (
            "AKR metadata header (<!-- akr-generated) not found. "
            "Skill may not have completed correctly."
        ),
        "line": 1,
    }]


def _check_required_sections(content: str) -> List[Dict]:
    """
    Check that all required sections are present.

    Discovery order:
    1. Read akr:section directives from the generated document.
    2. If none found, fall back to BASELINE_REQUIRED_SECTIONS.

    This keeps the inline validator in sync with template changes automatically
    for documents that carry directives forward from the template.
    """
    directive_sections = _extract_required_sections_from_directives(content)

    if directive_sections is not None:
        # Use directive-derived list
        required = directive_sections
        source = "directives"
    else:
        # Fallback to baseline list — convert to normalized form
        required = [_normalize_heading(s) for s in BASELINE_REQUIRED_SECTIONS]
        source = "baseline"

    headings = _extract_h2_headings(content)
    issues = []

    for section in required:
        norm = _normalize_heading(section) if source == "directives" else section
        if norm not in headings:
            readable = (
                _section_id_to_heading(section)
                if source == "directives"
                else section
            )
            issues.append({
                "severity": "error",
                "rule": "required-sections",
                "message": f"Missing required section: {readable}",
                "line": None,
            })

    return issues


def _check_transparency_markers(content: str, compliance_mode: str) -> List[Dict]:
    issues = []

    q_count = content.count("❓")
    bot_count = content.count("🤖")
    deferred_count = len(re.findall(r"\bDEFERRED\b", content, re.IGNORECASE))

    if q_count > 0:
        severity = "error" if compliance_mode == "production" else "warning"
        issues.append({
            "severity": severity,
            "rule": "transparency-markers",
            "message": (
                f"Found {q_count} unresolved ❓ marker(s). "
                + ("Blocking in production mode." if compliance_mode == "production"
                   else "Resolve before graduating to production mode.")
            ),
            "line": None,
        })

    if deferred_count > 0:
        issues.append({
            "severity": "warning",
            "rule": "transparency-markers",
            "message": (
                f"Found {deferred_count} DEFERRED marker(s). "
                "Verify each has an owner and follow-up trigger."
            ),
            "line": None,
        })

    if bot_count > 0:
        issues.append({
            "severity": "info",
            "rule": "transparency-markers",
            "message": f"Found {bot_count} 🤖 marker(s) — AI-generated content awaiting human review.",
            "line": None,
        })

    return issues


# ---------------------------------------------------------------------------
# Main validation entry point
# ---------------------------------------------------------------------------

def validate_file(
    file_path: Path,
    compliance_mode: Optional[str] = None,
    is_final: bool = True,
) -> Dict:
    if not file_path.exists():
        return {
            "file_path": str(file_path),
            "valid": False,
            "issues": [{
                "severity": "error",
                "rule": "file-not-found",
                "message": f"File not found: {file_path}",
                "line": None,
            }],
            "error_count": 1,
            "warning_count": 0,
        }

    content = file_path.read_text(encoding="utf-8")
    front_matter, fm_found = _parse_front_matter(content)

    # Read compliance_mode from front matter if not overridden by caller
    fm_compliance = front_matter.get("compliance_mode", "").strip()
    if compliance_mode is not None:
        effective_compliance = compliance_mode
    elif fm_compliance in VALID_COMPLIANCE_MODES:
        effective_compliance = fm_compliance
    else:
        effective_compliance = "pilot"

    issues: List[Dict] = []
    issues.extend(_check_front_matter(content, front_matter, fm_found, is_final))
    issues.extend(_check_metadata_header(content))
    issues.extend(_check_required_sections(content))
    issues.extend(_check_transparency_markers(content, effective_compliance))

    error_count = sum(1 for i in issues if i["severity"] == "error")
    warning_count = sum(1 for i in issues if i["severity"] == "warning")

    return {
        "file_path": str(file_path),
        "valid": error_count == 0,
        "compliance_mode": effective_compliance,
        "constants_version": CONSTANTS_VERSION,
        "error_count": error_count,
        "warning_count": warning_count,
        "issues": issues,
    }


def format_result(result: Dict) -> str:
    lines = [
        f"AKR Inline Validation: {result['file_path']}",
        f"Status:   {'✅ PASSED' if result['valid'] else '❌ FAILED'}",
        f"Errors:   {result['error_count']}",
        f"Warnings: {result['warning_count']}",
    ]
    if result["issues"]:
        lines.append("")
        for issue in result["issues"]:
            sev = issue["severity"].upper()
            loc = f" (line {issue['line']})" if issue.get("line") else ""
            lines.append(f"  [{sev}]{loc} [{issue['rule']}] {issue['message']}")
    lines.append("")
    if result["valid"]:
        lines.append(
            "✅ Inline checks passed. Open PR to trigger full CI validation.\n"
            "   Full CI checks: Vale linting, modules.yaml cross-refs, completeness scoring."
        )
    else:
        lines.append("❌ Fix the above errors before opening PR.")
    return "\n".join(lines)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(
        description="AKR inline post-generation validator (stdlib only, no external dependencies)"
    )
    parser.add_argument("file", help="Path to the generated markdown document")
    parser.add_argument(
        "--compliance-mode",
        choices=["pilot", "production"],
        default=None,
        help="Override compliance mode (default: read from document front matter)",
    )
    parser.add_argument(
        "--draft",
        action="store_true",
        help="Validate as draft artifact (skips draft-only field check)",
    )
    parser.add_argument(
        "--output",
        choices=["text", "json"],
        default="text",
    )
    parser.add_argument(
        "--constants-version",
        action="store_true",
        help="Print CONSTANTS_VERSION and exit (used by CI sync test)",
    )
    args = parser.parse_args()

    if args.constants_version:
        print(CONSTANTS_VERSION)
        return 0

    result = validate_file(
        Path(args.file),
        compliance_mode=args.compliance_mode,
        is_final=not args.draft,
    )

    if args.output == "json":
        print(json.dumps(result, indent=2))
    else:
        print(format_result(result))

    return 0 if result["valid"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

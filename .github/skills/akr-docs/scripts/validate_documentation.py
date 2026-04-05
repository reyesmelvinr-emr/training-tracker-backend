#!/usr/bin/env python3
"""AKR module-aware documentation validator (Phase 1 v1.0 kickoff)."""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Tuple

import yaml


MODULE_REQUIRED_SECTIONS = [
    "Overview",
    "Module Files",
    "Operations Map",
    "Architecture Overview",
    "Business Rules",
]

DB_REQUIRED_SECTIONS = [
    "Overview",
    "Schema",
    "Relationships",
]

GENERIC_REQUIRED_SECTIONS = ["Overview"]

PROJECT_LAYER_ENUM = {"UI", "API", "Database", "Integration", "Infrastructure", "Full-Stack"}
PROJECT_TYPE_ENUM = {"api-backend", "ui-component", "microservice", "general"}
MODULE_STATUS_ENUM = {"draft", "review", "approved", "in-progress", "deprecated"}
DB_TYPE_ENUM = {"table", "view", "procedure", "function", "schema"}
DRAFT_ONLY_FRONT_MATTER_FIELDS = {"preview-generated-at", "review-mode"}
MODULE_REQUIRED_FRONT_MATTER_FIELDS = {
    "businesscapability",
    "feature",
    "layer",
    "project_type",
    "status",
    "compliance_mode",
}


@dataclass
class ValidationIssue:
    severity: str
    message: str
    rule: str
    line: Optional[int] = None

    def to_dict(self) -> Dict[str, Any]:
        return {
            "severity": self.severity,
            "message": self.message,
            "rule": self.rule,
            "line": self.line,
        }


@dataclass
class ValidationResult:
    file_path: str
    doc_type: str
    module_name: Optional[str]
    valid: bool
    completeness_score: float
    issues: List[ValidationIssue] = field(default_factory=list)

    def error_count(self) -> int:
        return sum(1 for issue in self.issues if issue.severity == "error")

    def warning_count(self) -> int:
        return sum(1 for issue in self.issues if issue.severity == "warning")

    def to_dict(self) -> Dict[str, Any]:
        return {
            "file_path": self.file_path,
            "doc_type": self.doc_type,
            "module_name": self.module_name,
            "valid": self.valid,
            "completeness_score": self.completeness_score,
            "error_count": self.error_count(),
            "warning_count": self.warning_count(),
            "issues": [issue.to_dict() for issue in self.issues],
        }


def _normalize_heading(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", "", text.lower())


def _extract_h2_headings(content: str) -> List[Tuple[str, int]]:
    out: List[Tuple[str, int]] = []
    for idx, line in enumerate(content.splitlines(), start=1):
        if line.startswith("## "):
            out.append((line[3:].strip(), idx))
    return out


def _find_line_number(content: str, needle: str) -> Optional[int]:
    for idx, line in enumerate(content.splitlines(), start=1):
        if needle in line:
            return idx
    return None


def _split_changed_files(raw: str) -> List[str]:
    tokens: List[str] = []
    for part in re.split(r"[\s\r\n\t]+", raw.strip()):
        if part:
            tokens.append(part)
    return tokens


def _read_changed_files_from_env() -> List[str]:
    keys = [
        "CHANGED_FILES",
        "ALL_CHANGED_FILES",
        "CHANGED_FILES_LIST",
        "INPUT_CHANGED_FILES",
    ]
    for key in keys:
        value = os.environ.get(key, "").strip()
        if value:
            return _split_changed_files(value)
    return []


def _relative_posix(path: Path, workspace_root: Path) -> str:
    try:
        rel = path.resolve().relative_to(workspace_root.resolve())
    except ValueError:
        rel = path
    return rel.as_posix()


def _load_yaml(path: Path) -> Dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        loaded = yaml.safe_load(handle) or {}
    if not isinstance(loaded, dict):
        raise ValueError("modules.yaml must contain a mapping at top level")
    return loaded


def _parse_semver(value: Any) -> Optional[Tuple[int, int, int]]:
    if not isinstance(value, str):
        return None
    match = re.match(r"^v?(\d+)\.(\d+)\.(\d+)$", value.strip())
    if not match:
        return None
    return int(match.group(1)), int(match.group(2)), int(match.group(3))


def _extract_front_matter_fields(content: str) -> Dict[str, str]:
    lines = content.splitlines()
    if not lines or lines[0].strip() != "---":
        return {}

    fields: Dict[str, str] = {}
    for line in lines[1:]:
        if line.strip() == "---":
            break
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        fields[key.strip().lower()] = value.strip()
    return fields


def _collect_declared_artifact_warnings(manifest: Dict[str, Any], workspace_root: Path) -> List[ValidationIssue]:
    issues: List[ValidationIssue] = []
    for module in manifest.get("modules", []):
        if not isinstance(module, dict):
            continue

        draft_output = module.get("draft_output")
        if isinstance(draft_output, str) and draft_output.strip():
            draft_path = (workspace_root / draft_output).resolve()
            if not draft_path.exists():
                issues.append(
                    ValidationIssue(
                        "warning",
                        "Draft declared but not found. Run GenerateDocumentation.",
                        "declared-artifacts",
                    )
                )

        review_sheet = module.get("review_sheet")
        if isinstance(review_sheet, str) and review_sheet.strip():
            review_path = (workspace_root / review_sheet).resolve()
            if not review_path.exists():
                issues.append(
                    ValidationIssue(
                        "warning",
                        "Review sheet declared but not found. Run ProposeGroupings.",
                        "declared-artifacts",
                    )
                )

    return issues


def _validate_manifest_schema(manifest: Dict[str, Any]) -> List[ValidationIssue]:
    issues: List[ValidationIssue] = []

    project = manifest.get("project")
    modules = manifest.get("modules")
    database_objects = manifest.get("database_objects")
    unassigned = manifest.get("unassigned")

    if not isinstance(project, dict):
        issues.append(ValidationIssue("error", "modules.yaml missing required object: project", "modules-schema"))
        project = {}
    if not isinstance(modules, list):
        issues.append(ValidationIssue("error", "modules.yaml missing required array: modules", "modules-schema"))
        modules = []
    if not isinstance(database_objects, list):
        issues.append(ValidationIssue("error", "modules.yaml missing required array: database_objects", "modules-schema"))
        database_objects = []
    if not isinstance(unassigned, list):
        issues.append(ValidationIssue("error", "modules.yaml missing required array: unassigned", "modules-schema"))

    layer = project.get("layer")
    if layer not in PROJECT_LAYER_ENUM:
        issues.append(
            ValidationIssue(
                "error",
                "modules.yaml project.layer must be one of: UI, API, Database, Integration, Infrastructure, Full-Stack",
                "modules-schema",
            )
        )

    compliance_mode = project.get("compliance_mode")
    if compliance_mode not in {"pilot", "production"}:
        issues.append(ValidationIssue("error", "modules.yaml project.compliance_mode must be pilot or production", "modules-schema"))

    standards_version = _parse_semver(project.get("standards_version"))
    minimum_standards_version = _parse_semver(project.get("minimum_standards_version"))
    if standards_version and minimum_standards_version and standards_version < minimum_standards_version:
        issues.append(
            ValidationIssue(
                "error",
                "modules.yaml project.standards_version must be >= project.minimum_standards_version",
                "modules-schema",
            )
        )

    module_names: set[str] = set()
    doc_outputs: set[str] = set()

    for idx, module in enumerate(modules):
        if not isinstance(module, dict):
            issues.append(ValidationIssue("error", f"modules[{idx}] must be an object", "modules-schema"))
            continue

        name = module.get("name")
        if not isinstance(name, str) or not name.strip():
            issues.append(ValidationIssue("error", f"modules[{idx}].name is required", "modules-schema"))
        elif name in module_names:
            issues.append(ValidationIssue("error", f"Duplicate module name: {name}", "modules-schema"))
        else:
            module_names.add(name)

        project_type = module.get("project_type")
        if project_type not in PROJECT_TYPE_ENUM:
            issues.append(
                ValidationIssue(
                    "error",
                    f"modules[{idx}].project_type has unknown value: {project_type}",
                    "modules-schema",
                )
            )

        status = module.get("status")
        if status not in MODULE_STATUS_ENUM:
            issues.append(ValidationIssue("error", f"modules[{idx}].status has invalid value: {status}", "modules-schema"))

        max_files = module.get("max_files")
        files = module.get("files")
        if not isinstance(max_files, int) or max_files < 1 or max_files > 8:
            issues.append(ValidationIssue("error", f"modules[{idx}].max_files must be 1..8", "modules-schema"))
        if not isinstance(files, list) or len(files) == 0:
            issues.append(ValidationIssue("error", f"modules[{idx}].files must include at least one file", "modules-schema"))
        elif isinstance(max_files, int) and len(files) > max_files:
            issues.append(
                ValidationIssue(
                    "error",
                    f"modules[{idx}] exceeds max_files ({len(files)} > {max_files})",
                    "modules-schema",
                )
            )

        doc_output = module.get("doc_output")
        if not isinstance(doc_output, str) or not doc_output.startswith("docs/") or not doc_output.endswith(".md"):
            issues.append(ValidationIssue("error", f"modules[{idx}].doc_output must match docs/*.md", "modules-schema"))
        elif doc_output in doc_outputs:
            issues.append(ValidationIssue("error", f"Duplicate doc_output path: {doc_output}", "modules-schema"))
        else:
            doc_outputs.add(doc_output)

    for idx, item in enumerate(database_objects):
        if not isinstance(item, dict):
            issues.append(ValidationIssue("error", f"database_objects[{idx}] must be an object", "modules-schema"))
            continue
        obj_type = item.get("type")
        if obj_type not in DB_TYPE_ENUM:
            issues.append(ValidationIssue("error", f"database_objects[{idx}].type has invalid value: {obj_type}", "modules-schema"))

        doc_output = item.get("doc_output")
        if isinstance(doc_output, str):
            if doc_output in doc_outputs:
                issues.append(ValidationIssue("error", f"Duplicate doc_output path: {doc_output}", "modules-schema"))
            else:
                doc_outputs.add(doc_output)

    return issues


def _build_doc_index(manifest: Dict[str, Any]) -> Dict[str, Dict[str, Any]]:
    index: Dict[str, Dict[str, Any]] = {}

    for module in manifest.get("modules", []):
        if not isinstance(module, dict):
            continue
        doc_output = module.get("doc_output")
        if isinstance(doc_output, str):
            index[doc_output] = {
                "doc_type": "module",
                "module_name": module.get("name"),
                "project_type": module.get("project_type"),
                "doc_output": doc_output,
                "draft_output": module.get("draft_output"),
                "review_sheet": module.get("review_sheet"),
                "ssg_pass3_source_reread": bool(module.get("ssg_pass3_source_reread", False)),
                "ssg_pass4_source_reread": bool(module.get("ssg_pass4_source_reread", False)),
            }

    for item in manifest.get("database_objects", []):
        if not isinstance(item, dict):
            continue
        doc_output = item.get("doc_output")
        if isinstance(doc_output, str):
            index[doc_output] = {
                "doc_type": "database_object",
                "module_name": item.get("name"),
                "object_type": item.get("type"),
            }

    return index


def _check_required_sections(content: str, required_sections: List[str]) -> List[ValidationIssue]:
    issues: List[ValidationIssue] = []
    headings = _extract_h2_headings(content)
    heading_map = {_normalize_heading(name): line for name, line in headings}

    for section in required_sections:
        norm = _normalize_heading(section)
        if norm not in heading_map:
            issues.append(
                ValidationIssue(
                    "error",
                    f"Missing required section: {section}",
                    "required-sections",
                )
            )
    return issues


def _check_transparency_markers(content: str, compliance_mode: str) -> List[ValidationIssue]:
    issues: List[ValidationIssue] = []
    q_count = content.count("❓")
    ai_count = content.count("🤖")
    deferred_count = len(re.findall(r"\bDEFERRED\b", content, flags=re.IGNORECASE))

    if q_count > 0 and compliance_mode == "production":
        issues.append(
            ValidationIssue(
                "error",
                f"Found {q_count} unresolved ❓ marker(s) in production compliance mode",
                "transparency-markers",
            )
        )
    elif q_count > 0:
        issues.append(
            ValidationIssue(
                "warning",
                f"Found {q_count} unresolved ❓ marker(s) in pilot compliance mode",
                "transparency-markers",
            )
        )

    if deferred_count > 0:
        issues.append(
            ValidationIssue(
                "warning",
                f"Found {deferred_count} DEFERRED marker(s); verify deferred content ownership",
                "transparency-markers",
            )
        )

    if ai_count > 0:
        issues.append(
            ValidationIssue(
                "info",
                f"Found {ai_count} 🤖 marker(s) (informational)",
                "transparency-markers",
            )
        )

    return issues


def _check_akr_generated_header(content: str) -> List[ValidationIssue]:
    if re.search(r"<!--\s*akr-generated\b", content):
        return []
    return [
        ValidationIssue(
            "error",
            "AKR metadata header missing — skill may not have been properly invoked",
            "akr-generated-header",
            line=1,
        )
    ]


def _check_module_front_matter(content: str, front_matter: Dict[str, str]) -> List[ValidationIssue]:
    issues: List[ValidationIssue] = []

    if not front_matter:
        issues.append(
            ValidationIssue(
                "error",
                "Missing YAML front matter for module document",
                "front-matter",
                line=1,
            )
        )
        return issues

    missing = sorted(field for field in MODULE_REQUIRED_FRONT_MATTER_FIELDS if not front_matter.get(field, "").strip())
    for field in missing:
        issues.append(
            ValidationIssue(
                "error",
                f"Missing required front matter field: {field}",
                "front-matter",
                line=_find_line_number(content, f"{field}:") or 1,
            )
        )

    layer = front_matter.get("layer")
    if layer and layer not in PROJECT_LAYER_ENUM:
        issues.append(
            ValidationIssue(
                "error",
                f"Invalid front matter layer: {layer}",
                "front-matter",
                line=_find_line_number(content, "layer:") or 1,
            )
        )

    project_type = front_matter.get("project_type")
    if project_type and project_type not in PROJECT_TYPE_ENUM:
        issues.append(
            ValidationIssue(
                "error",
                f"Invalid front matter project_type: {project_type}",
                "front-matter",
                line=_find_line_number(content, "project_type:") or 1,
            )
        )

    status = front_matter.get("status")
    if status and status not in MODULE_STATUS_ENUM:
        issues.append(
            ValidationIssue(
                "error",
                f"Invalid front matter status: {status}",
                "front-matter",
                line=_find_line_number(content, "status:") or 1,
            )
        )

    compliance_mode = front_matter.get("compliance_mode")
    if compliance_mode and compliance_mode not in {"pilot", "production"}:
        issues.append(
            ValidationIssue(
                "error",
                f"Invalid front matter compliance_mode: {compliance_mode}",
                "front-matter",
                line=_find_line_number(content, "compliance_mode:") or 1,
            )
        )

    return issues


def _compute_completeness(required_sections: List[str], issues: List[ValidationIssue]) -> float:
    if not required_sections:
        base = 100.0
    else:
        missing = sum(1 for issue in issues if issue.rule == "required-sections" and issue.severity == "error")
        base = max(0.0, 100.0 * (len(required_sections) - missing) / len(required_sections))

    penalties = 0.0
    penalties += 15.0 * sum(1 for issue in issues if issue.severity == "error")
    penalties += 5.0 * sum(1 for issue in issues if issue.severity == "warning")

    return round(max(0.0, min(100.0, base - penalties)), 2)


def _classify_document(
    doc_path: Path,
    workspace_root: Path,
    doc_index: Dict[str, Dict[str, Any]],
) -> Tuple[str, Optional[str], Dict[str, Any]]:
    rel = _relative_posix(doc_path, workspace_root)
    info = doc_index.get(rel)
    if info:
        return info.get("doc_type", "generic"), info.get("module_name"), info
    return "generic", None, {}


def _collect_files(args: argparse.Namespace, workspace_root: Path) -> List[Path]:
    if args.file:
        file_path = (workspace_root / args.file).resolve() if not Path(args.file).is_absolute() else Path(args.file)
        return [file_path]

    if args.all:
        base = (workspace_root / args.all).resolve() if not Path(args.all).is_absolute() else Path(args.all)
        if not base.is_dir():
            raise ValueError(f"--all path is not a directory: {base}")
        return sorted(base.rglob("*.md"))

    if args.changed_files:
        changed = _read_changed_files_from_env()
        if not changed:
            return []
        files: List[Path] = []
        for rel in changed:
            if not rel.endswith(".md"):
                continue
            candidate = (workspace_root / rel).resolve()
            if candidate.exists():
                files.append(candidate)
        return files

    raise ValueError("Provide one of --file, --all, or --changed-files")


def _validate_single_file(
    doc_path: Path,
    workspace_root: Path,
    doc_index: Dict[str, Dict[str, Any]],
    compliance_mode: str,
) -> ValidationResult:
    text = doc_path.read_text(encoding="utf-8")

    doc_type, module_name, info = _classify_document(doc_path, workspace_root, doc_index)
    if doc_type == "module":
        required = MODULE_REQUIRED_SECTIONS
    elif doc_type == "database_object":
        required = DB_REQUIRED_SECTIONS
    else:
        required = GENERIC_REQUIRED_SECTIONS

    issues: List[ValidationIssue] = []
    issues.extend(_check_required_sections(text, required))
    issues.extend(_check_transparency_markers(text, compliance_mode))

    if doc_type == "module":
        issues.extend(_check_akr_generated_header(text))
        front_matter = _extract_front_matter_fields(text)
        issues.extend(_check_module_front_matter(text, front_matter))
        if info.get("doc_output") == _relative_posix(doc_path, workspace_root):
            if any(field in front_matter for field in DRAFT_ONLY_FRONT_MATTER_FIELDS):
                issues.append(
                    ValidationIssue(
                        "error",
                        "Final doc contains draft-only front matter fields. Re-run GenerateDocumentation Step 6a to strip before committing.",
                        "final-doc-cleanliness",
                    )
                )
        if info.get("ssg_pass4_source_reread"):
            issues.append(ValidationIssue("info", "Module override enabled: pass4-override", "pass4-override"))
        if info.get("ssg_pass3_source_reread"):
            issues.append(ValidationIssue("info", "Module override enabled: pass3-override", "pass3-override"))

    score = _compute_completeness(required, issues)
    valid = all(issue.severity != "error" for issue in issues)

    return ValidationResult(
        file_path=_relative_posix(doc_path, workspace_root),
        doc_type=doc_type,
        module_name=module_name,
        valid=valid,
        completeness_score=score,
        issues=issues,
    )


def _format_text_output(results: List[ValidationResult], summary: Dict[str, Any], preflight_issues: List[ValidationIssue]) -> str:
    lines: List[str] = []

    for issue in preflight_issues:
        lines.append(f"[{issue.severity.upper()}] {issue.rule}: {issue.message}")

    for result in results:
        lines.append("=" * 80)
        lines.append(f"File: {result.file_path}")
        lines.append(f"Doc Type: {result.doc_type}")
        lines.append(f"Status: {'VALID' if result.valid else 'INVALID'}")
        lines.append(f"Completeness: {result.completeness_score:.2f}")
        lines.append(f"Errors: {result.error_count()}, Warnings: {result.warning_count()}")
        for issue in result.issues:
            loc = f" (line {issue.line})" if issue.line else ""
            lines.append(f"  - {issue.severity.upper()}{loc} [{issue.rule}] {issue.message}")

    lines.append("=" * 80)
    lines.append("Summary:")
    lines.append(f"  Total files: {summary['total_files']}")
    lines.append(f"  Valid files: {summary['valid_files']}")
    lines.append(f"  Invalid files: {summary['invalid_files']}")
    lines.append(f"  Total errors: {summary['total_errors']}")
    lines.append(f"  Total warnings: {summary['total_warnings']}")
    lines.append(f"  Average completeness: {summary['average_completeness']}")

    return "\n".join(lines)


def _format_preview_output(
    results: List[ValidationResult],
    preflight_issues: List[ValidationIssue],
    workspace_root: Path,
) -> str:
    lines: List[str] = []
    for issue in preflight_issues:
        lines.append(f"[{issue.severity.upper()}] {issue.rule}: {issue.message}")

    for result in results:
        file_path = (workspace_root / result.file_path).resolve()
        section_map = {
            _normalize_heading(name): line
            for name, line in _extract_h2_headings(file_path.read_text(encoding="utf-8"))
        }
        required_sections = MODULE_REQUIRED_SECTIONS if result.doc_type == "module" else DB_REQUIRED_SECTIONS if result.doc_type == "database_object" else GENERIC_REQUIRED_SECTIONS
        section_status = " | ".join(
            f"{name} {'OK' if _normalize_heading(name) in section_map else 'MISSING'}" for name in required_sections
        )
        question_count = sum(1 for issue in result.issues if issue.rule == "transparency-markers" and "marker(s)" in issue.message)
        inferred_count = sum(1 for issue in result.issues if issue.rule == "transparency-markers" and "informational" in issue.message)

        content = file_path.read_text(encoding="utf-8")
        front_matter = _extract_front_matter_fields(content)
        generation_strategy = front_matter.get("generation-strategy", "unavailable")
        review_mode = front_matter.get("review-mode", "unavailable")

        lines.append(f"GenerateDocumentation Preview: {result.module_name or result.file_path}")
        lines.append(f"Sections present:     {section_status}")
        lines.append(f"Question markers:    {question_count} sections require human input")
        lines.append(f"AI inferred content: {inferred_count} items flagged")
        lines.append(f"Validator result:     {result.error_count()} errors / {result.warning_count()} warnings")
        lines.append(f"Generation strategy:  {generation_strategy}")
        lines.append(f"Review mode:          {review_mode}")
        lines.append("--------------------------------------------------------------")

    return "\n".join(lines)


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Validate AKR documentation with module-aware rules")
    parser.add_argument("--file", help="Single markdown file to validate")
    parser.add_argument("--all", help="Validate all markdown files under this directory")
    parser.add_argument("--changed-files", action="store_true", help="Validate markdown files from changed-files environment output")
    parser.add_argument("--module-name", help="Optional module name filter when modules.yaml exists")
    parser.add_argument("--workspace-root", default=".", help="Workspace root path (default: current directory)")
    parser.add_argument("--preview", action="store_true", help="Preview mode output for draft review flows")
    parser.add_argument(
        "--fail-on",
        choices=["errors", "warnings", "never", "needs", "all"],
        default="errors",
        help="Exit behavior. Aliases: needs=errors, all=warnings",
    )
    parser.add_argument("--output", choices=["text", "json"], default="text")
    return parser


def main() -> int:
    parser = _build_parser()
    args = parser.parse_args()

    workspace_root = Path(args.workspace_root).resolve()
    preflight_issues: List[ValidationIssue] = []

    modules_yaml_path = workspace_root / "modules.yaml"
    manifest: Dict[str, Any] = {}
    doc_index: Dict[str, Dict[str, Any]] = {}
    compliance_mode = "pilot"

    if modules_yaml_path.exists():
        try:
            manifest = _load_yaml(modules_yaml_path)
            preflight_issues.extend(_validate_manifest_schema(manifest))
            preflight_issues.extend(_collect_declared_artifact_warnings(manifest, workspace_root))
            doc_index = _build_doc_index(manifest)
            compliance_mode = manifest.get("project", {}).get("compliance_mode", "pilot")
        except Exception as exc:  # noqa: BLE001
            preflight_issues.append(ValidationIssue("error", f"Failed to parse modules.yaml: {exc}", "modules-schema"))
    else:
        preflight_issues.append(
            ValidationIssue(
                "warning",
                "modules.yaml not found; applying generic fallback validation rules",
                "modules-yaml-absent",
            )
        )

    try:
        files = _collect_files(args, workspace_root)
    except ValueError as exc:
        print(str(exc), file=sys.stderr)
        return 2

    if args.module_name and doc_index:
        allowed = {
            path
            for path, item in doc_index.items()
            if item.get("doc_type") == "module" and item.get("module_name") == args.module_name
        }
        files = [path for path in files if _relative_posix(path, workspace_root) in allowed]

    results = [_validate_single_file(path, workspace_root, doc_index, compliance_mode) for path in files]

    total_errors = sum(result.error_count() for result in results) + sum(
        1 for issue in preflight_issues if issue.severity == "error"
    )
    total_warnings = sum(result.warning_count() for result in results) + sum(
        1 for issue in preflight_issues if issue.severity == "warning"
    )

    summary = {
        "total_files": len(results),
        "valid_files": sum(1 for result in results if result.valid),
        "invalid_files": sum(1 for result in results if not result.valid),
        "total_errors": total_errors,
        "total_warnings": total_warnings,
        "average_completeness": round(
            sum(result.completeness_score for result in results) / len(results), 2
        )
        if results
        else 0.0,
    }

    payload = {
        "summary": summary,
        "preflight_issues": [issue.to_dict() for issue in preflight_issues],
        "results": [result.to_dict() for result in results],
    }

    if args.preview:
        print(_format_preview_output(results, preflight_issues, workspace_root))
    elif args.output == "json":
        print(json.dumps(payload, indent=2))
    else:
        print(_format_text_output(results, summary, preflight_issues))

    fail_on = args.fail_on
    if fail_on == "needs":
        fail_on = "errors"
    elif fail_on == "all":
        fail_on = "warnings"

    if fail_on == "never":
        return 0
    if fail_on == "warnings" and (summary["total_errors"] > 0 or summary["total_warnings"] > 0):
        return 1
    if fail_on == "errors" and summary["total_errors"] > 0:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

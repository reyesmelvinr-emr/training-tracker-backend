## AKR Documentation PR

Module: <!-- e.g., CourseDomain -->
Project type: <!-- api-backend | ui-component | microservice -->
Generation mode: <!-- Mode B full | Mode B incremental -->
Skill version used: <!-- from the akr-generated metadata header -->

---

### Developer Review Checklist

Do not check a box unless you have personally verified the item.

#### Structural (CI-enforced)
- [ ] AKR metadata header is present at top of document
- [ ] All required sections are present (Module Files, Operations Map, Architecture Overview, Business Rules)
- [ ] YAML front matter is complete (businessCapability, feature, layer, project_type, status, compliance_mode)
- [ ] CI validation check passes

#### Content (developer responsibility)
- [ ] I reviewed the committed draft at docs/modules/.akr/{ModuleName}_draft.md before confirming ready to commit
- [ ] I filled or explicitly deferred all question-marker sections within my domain knowledge
- [ ] The Operations Map reflects the actual operations in source files
- [ ] The Business Rules section captures real business logic, not generic placeholders
- [ ] Any deferred items include owner and follow-up trigger

#### Incremental updates only
- [ ] Only sections affected by the code change were patched
- [ ] last_reviewed_at in modules.yaml was updated

---

### Reviewer Notes

Optional: highlight areas the tech lead should focus on.

# training-tracker-backend
.NET API backend for the Training Tracker POC with OpenAPI documentation

## AKR Skill Setup (Required)

Before running AKR hook validation or `/akr-docs` workflows, clone the templates cache locally:

```bash
git clone https://github.com/reyesmelvinr-emr/core-akr-templates ~/.akr/templates
```

Validation hooks in this repository expect:

- `~/.akr/templates/.akr/scripts/validate_documentation.py`
- `~/.akr/templates/.akr/.vale.ini`

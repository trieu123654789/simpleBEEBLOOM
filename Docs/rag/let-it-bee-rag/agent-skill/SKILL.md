---
name: let-it-bee-vr-performance-audit
description: Read-only, evidence-first Unity 6 Meta Quest 3/3S performance auditing for Let It Bee using XR Interaction Toolkit, OpenXR, URP and GAMA/SIMPLE.
---

# Workflow
1. Read README, sources-manifest and relevant knowledge notes. Verify actual project packages and architecture.
2. Inventory build settings, scenes, URP, XR settings, scripts, materials, assets and profiling artifacts.
3. Identify hypotheses with file paths and line numbers; distinguish static review from measurements.
4. Ask for on-device benchmark if missing; do not fabricate numbers.
5. Rank changes by evidence, likely impact, effort, regression risk and VR comfort.
6. Produce audit and proposed patch plan; **do not modify project without approval**.
7. After approval, implement isolated changes, test and record before/after measurements.

## Hard constraints
- Preserve XR Origin, eye/controller tracking, OpenXR and XR Interaction Toolkit setup.
- Never blindly introduce Meta OVRCameraRig/OVRManager into an XRI setup.
- Protect GAMA/SIMPLE message contract and Unity main-thread API restrictions.
- Cite source ID and URL; flag unverified compatibility or conflicting numerical budgets.
- Do not claim performance improvements from static code review.

## Output schema
Finding ID | Severity | Scene/system | File:line | Evidence | Hypothesis | Suggested experiment | Risk | Source IDs | Verification status.

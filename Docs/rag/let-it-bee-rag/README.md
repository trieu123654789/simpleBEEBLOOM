# Let It Bee — Unity 6 Quest Performance RAG

A source-grounded knowledge starter kit for Unity 6, URP, OpenXR, XR Interaction Toolkit and standalone Meta Quest 3/3S. This is a curated original synthesis and ingestion framework, **not** a mirror of the full copyrighted ebooks/docs.

## Start here
1. Read `sources-manifest.md`; download the Unity ebook yourself from its official landing page where permitted.
2. Point your agent at `agent-skill/SKILL.md` and `knowledge/*.md`.
3. Copy `AGENTS.md` to the Unity repository root, adjusting paths as needed.
4. Run the read-only audit prompt in `templates/first-audit-prompt.md`.
5. Record on-device baselines using `templates/benchmark.csv` before approving code changes.

## RAG ingestion
Chunk by heading (roughly 400–900 tokens), preserve document title, source URL, version, platform, retrieval date, confidence and last-verified date. Prefer exact source citation and avoid copying large passages from official docs. Recheck documentation when a recommendation depends on plugin versions. Never treat an LLM-generated summary as profiler evidence.

## Compatibility guardrail
This project already uses XR Interaction Toolkit + OpenXR. Do not blindly apply Meta SDK-specific OVRCameraRig/OVRManager setup instructions, create a second XR Origin, or change XR tracking hierarchy.

# Project Overview

## Summary
This Unity project is a VR/desktop educational simulation centered on seasonal salinity changes and farming outcomes. Gameplay focuses on planting crops, raising livestock, and fishing while adapting to rainy vs dry seasons. The project uses XR Interaction Toolkit for VR interaction and includes standard desktop input fallbacks.

## Core Scene
- Main gameplay scene: [Assets/Scenes/VU2/SCN_VU2_Level1_New.unity](Assets/Scenes/VU2/SCN_VU2_Level1_New.unity)

## Main Feature Pillars
- **Seasonal cycle & salinity** driving weather, visuals, and scoring.
- **Farm areas** that manage planting, growth, harvesting, and area-specific salinity.
- **UI/HUD** for progress, salinity readouts, seasonal scoreboards, and summaries.
- **NPC dialogue** and localized UI text.
- **Ambient systems** such as skybox rotation, weather following player, and pet idle AI.

## Code Organization (high-level)
- VU2 gameplay and managers: [Assets/Scripts/VU2](Assets/Scripts/VU2)
- Data models for JSON-driven content: [Assets/Scripts/VU2/Systems/Data](Assets/Scripts/VU2/Systems/Data)
- Legacy/other gameplay from VU1: [Assets/Scripts/VU1](Assets/Scripts/VU1)
- XR dependencies: [Packages/manifest.json](Packages/manifest.json)

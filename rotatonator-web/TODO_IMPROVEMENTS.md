# Rotatonator Web - Development Todos

## UI/UX Improvements

### 1. Healer List and Chain Timing Editor (PRIORITY: Medium)
**Status**: Not Started
**Context**: The current ConfigPanel uses simple list add/remove for healers and numeric input for chain interval. These could be significantly more user-friendly.

**Improvements to Explore**:
- Drag-and-drop reordering of healers to set rotation order
- Visual healer chain display (1st healer, 2nd healer, ...) during configuration
- Graphical chain timing editor (show seconds between healers visually)
- Import/export healer list from pasteable text format
- Preset healer lists for common group compositions  

**Dependencies**: 
- Existing ConfigPanel working (✅ DONE)
- Auto-cast removed (✅ DONE)

**Acceptance Criteria**:
- Healer order can be rearranged intuitively
- Chain timing is visualized
- Configuration remains persistent in localStorage
- Changes apply immediately to parser

---

## Parser & Rotation State

### 2. RotationManager Port to Web (TypeScript)
**Status**: Not Started
**Note**: Desktop LogMonitor and RotationManager need web equivalents to track healing order, timing, and current position.

### 3. Overlay Window UI Component
**Status**: Not Started
**Note**: Transparent overlay displaying active healer and chain status (Phase 2 of architecture).

---

## File Access

### 4. Companion Service Fallback (Phase 3+)
**Status**: Not Started  
**Note**: Browser File System Access API works great but optional: implement companion service for browsers without support.

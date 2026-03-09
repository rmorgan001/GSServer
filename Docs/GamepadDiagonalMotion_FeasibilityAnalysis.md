# Gamepad Diagonal Motion - Feasibility Analysis

**Project**: GSServer  
**Component**: Gamepad Hand Controller  
**Date**: 2026  
**Author**: AI Technical Analysis  
**Status**: Feasibility Assessment Complete — All Three Layers Analysed

---

## Executive Summary

This document provides a comprehensive feasibility analysis for implementing **diagonal motion support** in the GSServer gamepad hand controller system. The requirement is to enable simultaneous dual-axis movement when users press diagonal direction combinations (e.g., North + West).

**Overall Verdict**: **HIGHLY FEASIBLE** across all three implementation layers

| Layer | Complexity | Key Finding |
|-------|-----------|-------------|
| Mount control (`SkyServer.HcMoves`) | 3/10 | Dual-axis infrastructure already exists |
| Gamepad input (`GamePad` folder) | 3/10 | Hardware supports multi-button; single `break` is the only bottleneck |
| UI hand controller buttons | 4/10 | Grid corners are physically empty — zero structural layout work required |

All three layers are well-positioned to support this enhancement with minimal architectural changes. The most significant shared dependency is extending `SlewDirection` to cover diagonal values — a change that benefits all three layers simultaneously.

---

## Table of Contents

1. [Requirements Overview](#requirements-overview)
2. [Architecture Analysis](#architecture-analysis)
   - [SkyServer.HcMoves Layer](#skyserverhcmoves-layer)
   - [GamePad Input Layer](#gamepad-input-layer)
   - [UI Hand Controller Buttons Layer](#ui-hand-controller-buttons-layer)
3. [Current Limitations](#current-limitations)
4. [Proposed Solution](#proposed-solution)
5. [Implementation Details](#implementation-details)
6. [Testing Strategy](#testing-strategy)
7. [Risk Assessment](#risk-assessment)
8. [Recommendations](#recommendations)
9. [Appendix](#appendix)
   - [A. Code Snippets](#a-code-snippets)
   - [B. Directional Combinations Matrix](#b-directional-combinations-matrix)
   - [C. Alignment Mode Considerations](#c-alignment-mode-considerations)
   - [D. References](#d-references)
   - [E. UI Layer Implementation Pattern](#e-ui-layer-implementation-pattern)

---

## Requirements Overview

### User Story
As a telescope operator, I want to move the mount diagonally using the gamepad hand controller so that I can position the telescope more efficiently.

### Functional Requirements
1. **Detection**: Detect when both a vertical direction (North/South/Up/Down) and horizontal direction (East/West/Left/Right) button are pressed simultaneously
2. **Movement**: Move both telescope axes at their configured rates when diagonal input is detected
3. **State Transitions**: Appropriately adjust axis rates when transitioning between:
   - No movement → Diagonal movement
   - Diagonal movement → Single-axis movement
   - Single-axis movement → Diagonal movement
   - Diagonal movement → No movement

### Technical Constraints
- **C# Version**: 7.3
- **Framework**: .NET Framework 4.7.2
- **Project Type**: WPF Application
- **Compatibility**: Must work with existing alignment modes (AltAz, Polar, German Polar)
- **Anti-Backlash**: Must integrate with existing anti-backlash compensation

---

## Architecture Analysis

### SkyServer.HcMoves Layer

**File**: `GS.Server\SkyTelescope\SkyServer.cs`  
**Method**: `HcMoves(SlewSpeed speed, SlewDirection direction, HcMode hcMode, bool hcAntiRa, bool hcAntiDec, int raBacklash, int decBacklash)`

#### Current State
- Accepts **single** `SlewDirection` enum value
- Processes one direction at a time through switch statement
- Calculates `change[0]` (X-axis) and `change[1]` (Y-axis) independently
- Already has infrastructure for dual-axis movement

#### Capabilities Confirmed
✅ **Dual-axis output infrastructure exists**
- `change[0]` and `change[1]` arrays can hold simultaneous values
- Mount commands support simultaneous axis slewing:
  - Simulator: `CmdHcSlew(0, Axis.Axis1, change[0])` and `CmdHcSlew(0, Axis.Axis2, change[1])`
  - SkyWatcher: `SkyAxisSlew(0, AxisId.Axis1, rate.X)` and `SkyAxisSlew(0, AxisId.Axis2, rate.Y)`

#### Required Changes
1. **Input Parameter**: Change from single `SlewDirection` to accept multiple directions (flags or array)
2. **Switch Logic**: Modify to accumulate changes instead of single selection
3. **Anti-Backlash**: Ensure compensation works with diagonal movements

#### Complexity Assessment
- **Detection**: Already handled by gamepad layer
- **Calculation**: Modify switch statement logic (LOW complexity)
- **Integration**: Minimal changes to mount commands (LOW complexity)
- **Overall**: 3/10 complexity

---

### GamePad Input Layer

**Key Files**:
- `GS.Server\Gamepad\IGamepad.cs` - Interface definition
- `GS.Server\Gamepad\Gamepad.cs` - Abstract base class
- `GS.Server\Gamepad\GamepadVM.cs` - Main ViewModel with polling loop

#### Current State

**IGamepad Interface**:
```csharp
bool[] Buttons { get; }
int[] POVs { get; }
int? X { get; } // and Y, Z, XRotation, YRotation, ZRotation
```
- Exposes all button states simultaneously
- Hardware capable of detecting multiple pressed buttons

**GamePadLoopAsync Polling**:
```csharp
for (var i = 0; i < gamepadButtons.Length; i++) {
    if (!gamepadButtons[i]) continue;
    buttontocheck = DoGamePadCommand(i, gamepadButtons[i], cmd);
    break;  // ← EXITS AFTER FIRST BUTTON
}
```
- **Critical Issue**: Loop breaks after first pressed button
- Can detect multiple buttons but doesn't process them

**DoGamePadCommand Method**:
```csharp
case "up":
    if (value) {
        _skyTelescopeVM.HcMouseDownUpCommand.Execute(null);
    }
    break;
case "down": // Similar
case "left": // Similar
case "right": // Similar
```
- Processes each direction individually
- Triggers separate HC command per button
- No aggregation of simultaneous inputs

#### Required Changes

**Modification Point #1: Polling Loop Aggregation**
- **File**: `GamepadVM.cs` (lines ~600-750)
- **Current**: Loop breaks after first button
- **Change**: Remove `break`, accumulate all active directional buttons
- **Complexity**: LOW

**Modification Point #2: Direction Aggregation Logic**
- **File**: `GamepadVM.cs` (~900-1200)
- **Current**: `DoGamePadCommand` handles single direction
- **Change**: Create direction state accumulator, detect diagonal combinations
- **Complexity**: LOW-MODERATE

**Modification Point #3: Command Routing**
- **File**: `GamepadVM.cs` (~1000-1400)
- **Current**: Individual `HcMouseDown/Up` commands per direction
- **Options**:
  - **Option A**: Call two separate HC commands (up + left) - LOW complexity
  - **Option B**: Create new diagonal HC commands - MODERATE complexity
  - **Option C**: Modify command signature to accept direction flags - MODERATE complexity

**Modification Point #4: State Management**
- **File**: `GamepadVM.cs` (~900)
- **Current**: `pressedState` dictionary tracks individual buttons
- **Change**: Track combined directional state
- **Complexity**: LOW

---

### UI Hand Controller Buttons Layer

**Key Files**:
- `GS.Server\Controls\HandController.xaml` — Shared directional button UserControl
- `GS.Server\Windows\HandControlVM.cs` — Commands for floating HC window context
- `GS.Server\SkyTelescope\SkyTelescopeVM.cs` — Commands for main window context
- `GS.Server\Windows\HandControlV.xaml` — Floating HC window host
- `GS.Server\SkyTelescope\SkyTelescopeV.xaml` — Main window (embeds the UserControl)

#### Current State

**HandController.xaml Grid Layout**:

The directional buttons occupy a **3-row × 3-column sub-grid** within the control (which also has two additional auto-width columns for utility buttons and a speed slider). Current occupancy:

```
[Col 0]    [Col 1]    [Col 2]
           [ N  ]              ← Row 0
[ W  ]     [STOP]    [ E  ]   ← Row 1
           [ S  ]              ← Row 2
```

Corner positions `(Row 0, Col 0)`, `(Row 0, Col 2)`, `(Row 2, Col 0)`, and `(Row 2, Col 2)` are **completely empty**. These map directly to NW, NE, SW, and SE diagonal positions.

**Button Binding Pattern** (identical for all four cardinal buttons):
```xml
<Button Grid.Row="0" Grid.Column="1" Width="50" Height="50">
    <b:Interaction.Triggers>
        <b:EventTrigger EventName="PreviewMouseLeftButtonDown">
            <b:InvokeCommandAction Command="{Binding HcMouseDownUpCommand}" PassEventArgsToCommand="True" />
        </b:EventTrigger>
        <b:EventTrigger EventName="PreviewMouseLeftButtonUp">
            <b:InvokeCommandAction Command="{Binding HcMouseUpUpCommand}" PassEventArgsToCommand="True" />
        </b:EventTrigger>
    </b:Interaction.Triggers>
    <md:PackIcon Kind="ArrowUp" />
</Button>
```

**Dual DataContext — Critical Structural Finding**:

`HandController.xaml` is embedded in two separate contexts:
1. `SkyTelescopeV.xaml` (main window) → DataContext is `SkyTelescopeVm`
2. `HandControlV.xaml` (floating window) → DataContext is `HandControlVm`

All command bindings in the XAML (e.g. `HcMouseDownUpCommand`) must therefore exist in **both** ViewModels. This duplication pattern is already established for all eight existing cardinal commands.

**StartSlew Dispatch**:

Both ViewModels contain an identical `StartSlew(SlewDirection direction)` method that routes to `SkyServer.HcMoves`. The current `switch` has no diagonal cases and will throw `ArgumentOutOfRangeException` for any unknown direction value:

```csharp
private static void StartSlew(SlewDirection direction) {
    switch (direction) {
        case SlewDirection.SlewEast:
        case SlewDirection.SlewRight:    // ...
        case SlewDirection.SlewNoneRa:   // ...
        case SlewDirection.SlewNoneDec:  // ...
        default:
            throw new ArgumentOutOfRangeException();  // ← blocks diagonals
    }
}
```

#### Capabilities Confirmed

✅ **Grid corners are physically empty** — no layout restructuring needed  
✅ **MaterialDesign icon set covers all four diagonal arrows** — `ArrowTopLeft`, `ArrowTopRight`, `ArrowBottomLeft`, `ArrowBottomRight` (`md:PackIcon`)  
✅ **Control and window sizes are adequate** — 50×50 diagonal buttons match existing cardinals; no resize required  
✅ **`PreviewMouseLeftButtonDown/Up` event pattern** is directly reusable for diagonal buttons  
✅ **Flip logic is combinable** — diagonal commands apply both `FlipNs` and `FlipEw` corrections independently  

#### Required Changes

**Modification Point #1: HandController.xaml — Four Diagonal Buttons**
- **File**: `HandController.xaml`
- **Change**: Add buttons at `(Row 0, Col 0)`, `(Row 0, Col 2)`, `(Row 2, Col 0)`, `(Row 2, Col 2)` with `PreviewMouseLeftButtonDown/Up` bindings to new diagonal commands
- **Icons**: `ArrowTopLeft`, `ArrowTopRight`, `ArrowBottomLeft`, `ArrowBottomRight`
- **Complexity**: LOW

**Modification Point #2: SlewDirection Enum — Four New Values**
- **File**: `Enums.cs`
- **Change**: Add `SlewNorthEast`, `SlewNorthWest`, `SlewSouthEast`, `SlewSouthWest`
- **Alternative**: Use Option A (two sequential `StartSlew` calls) to avoid enum changes
- **Complexity**: VERY LOW

**Modification Point #3: SkyTelescopeVm — Eight New Commands**
- **File**: `SkyTelescopeVM.cs`
- **Change**: Add four `HcMouseDown{Diagonal}Command` + four `HcMouseUp{Diagonal}Command` properties and handler methods, each calling `StartSlew` with the correct combined direction and flip correction
- **Stop logic**: MouseUp handlers must stop **both** axes (`SlewNoneRa` + `SlewNoneDec`)
- **Complexity**: LOW-MODERATE (mechanical repetition of existing pattern)

**Modification Point #4: HandControlVm — Eight New Commands (Duplication)**
- **File**: `HandControlVM.cs`
- **Change**: Identical additions to Modification Point #3 — unavoidable due to dual-DataContext architecture
- **Complexity**: LOW-MODERATE

**Modification Point #5: Tooltip Properties**
- **Files**: `SkyTelescopeVM.cs`, `HandControlVM.cs`, resource dictionary
- **Change**: Add `HcToolTipNW`, `HcToolTipNE`, `HcToolTipSW`, `HcToolTipSE` string properties and corresponding resource strings
- **Complexity**: VERY LOW

#### Complexity Assessment
- **XAML layout**: Zero structural changes, 4 button blocks — LOW (1/10)
- **New enum values**: 4 lines — VERY LOW (1/10)
- **Command definitions**: Mechanical repetition across 2 VMs — LOW-MODERATE (3/10)
- **Stop-both-axes logic**: Two calls on MouseUp instead of one — LOW (2/10)
- **Overall UI Layer**: **4/10 complexity**

---

## Current Limitations

### 1. Sequential Button Processing
**Issue**: Polling loop exits after first pressed button  
**Impact**: Cannot detect simultaneous diagonal button presses  
**Location**: `GamepadVM.cs` GamePadLoopAsync method

### 2. Individual Command Execution
**Issue**: Each direction triggers separate command to HandControlVM  
**Impact**: Mount receives sequential commands instead of simultaneous movement  
**Location**: `GamepadVM.cs` DoGamePadCommand method

### 3. Single Direction Parameter
**Issue**: `HcMoves` accepts single `SlewDirection` enum  
**Impact**: Cannot receive diagonal direction information  
**Location**: `SkyServer.cs` HcMoves method signature

### 4. State Tracking Granularity
**Issue**: `pressedState` tracks individual buttons, not combinations  
**Impact**: Cannot prevent repeated diagonal commands while held  
**Location**: `GamepadVM.cs` button state management

### 5. Single-Direction StartSlew Dispatch
**Issue**: `StartSlew(SlewDirection)` in both `SkyTelescopeVm` and `HandControlVm` accepts only one direction; the `default` case throws `ArgumentOutOfRangeException`  
**Impact**: Diagonal button presses cannot be dispatched without a new overload or new enum values  
**Location**: `SkyTelescopeVM.cs` and `HandControlVM.cs` — `StartSlew` method

### 6. MouseUp Stops Only One Axis
**Issue**: Cardinal button `MouseUp` handlers call either `SlewNoneRa` or `SlewNoneDec`, not both  
**Impact**: A diagonal button release must stop both axes simultaneously — no existing mechanism does this  
**Location**: `HandControlVM.cs` and `SkyTelescopeVM.cs` — `HcMouseUp{Direction}` handlers

### 7. Commands Duplicated Across Two ViewModels
**Issue**: `HandController.xaml` inherits its DataContext from two different parent contexts (`SkyTelescopeVm` and `HandControlVm`)  
**Impact**: Every new diagonal command must be authored twice — once per ViewModel  
**Location**: `SkyTelescopeVM.cs` and `HandControlVM.cs`

---

## Proposed Solution

### Architecture: Hybrid Approach

#### Phase 1: Detection Layer (GamepadVM.cs)
1. Modify polling loop to accumulate active directional buttons
2. Create `ActiveDirections` helper class:

```csharp
class ActiveDirections {
    public bool Up { get; set; }
    public bool Down { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
    
    public bool IsDiagonal => (Up || Down) && (Left || Right);
    
    public SlewDirection[] GetDirections() {
        var directions = new List<SlewDirection>();
        if (Up) directions.Add(SlewDirection.SlewUp);
        if (Down) directions.Add(SlewDirection.SlewDown);
        if (Left) directions.Add(SlewDirection.SlewLeft);
        if (Right) directions.Add(SlewDirection.SlewRight);
        return directions.ToArray();
    }
}
```

#### Phase 2: Command Routing (GamepadVM.cs)
**Recommended**: Use Option A (Sequential Commands)
- Call existing commands in sequence for diagonal moves
- Example for Northwest: Execute `HcMouseDownUpCommand` then `HcMouseDownLeftCommand`
- Minimal code changes, leverages existing infrastructure

**Alternative**: Option B (New Diagonal Commands)
- Create dedicated commands: `HcMouseDownNorthWest`, `HcMouseDownNorthEast`, etc.
- Cleaner semantics, single command call
- Requires HandControlVM modifications

#### Phase 3: State Management
Extend `pressedState` dictionary to track combinations:
```csharp
// Track combined state
string combinedKey = GetCombinedKey(activeDirections);
if (!GetPressedState(combinedKey)) {
    SetPressedState(combinedKey, true);
    // Execute commands
}
```

#### Phase 4: Release Logic
Handle partial releases gracefully:
- Release left while up still pressed → Transition from diagonal to vertical
- Track previous state to determine which commands to stop/start

#### Phase 5: SkyServer.HcMoves Enhancement
Modify signature to accept multiple directions:
```csharp
public static void HcMoves(
    SlewSpeed speed, 
    SlewDirection[] directions,  // Changed from single direction
    HcMode hcMode, 
    bool hcAntiRa, 
    bool hcAntiDec, 
    int raBacklash, 
    int decBacklash)
{
    // Accumulate changes for all active directions
    foreach (var direction in directions) {
        switch (direction) {
            case SlewDirection.SlewNorth:
            case SlewDirection.SlewUp:
                change[1] = delta;
                break;
            case SlewDirection.SlewEast:
            case SlewDirection.SlewLeft:
                change[0] = delta;
                break;
            // etc.
        }
    }
    // Existing mount command logic works unchanged
}
```

---

## Implementation Details

### Component-by-Component Effort Estimates

#### Gamepad + SkyServer Layer

| Component | File | Complexity | Effort | Risk |
|-----------|------|-----------|--------|------|
| Polling Loop Modification | GamepadVM.cs | LOW | 1-2 hours | LOW |
| Direction Aggregation | GamepadVM.cs | LOW-MODERATE | 2-4 hours | LOW |
| Command Routing (Option A) | GamepadVM.cs | LOW | 2-3 hours | LOW |
| Command Routing (Option B) | GamepadVM.cs, HandControlVM.cs | MODERATE | 4-6 hours | MODERATE |
| State Management | GamepadVM.cs | LOW | 1-2 hours | LOW |
| HcMoves Signature Change | SkyServer.cs | LOW | 1-2 hours | LOW |
| Anti-Backlash Integration | SkyServer.cs | LOW | 1-2 hours | LOW |
| Testing & Validation | Various | MODERATE | 4-6 hours | LOW |

#### UI Hand Controller Buttons Layer

| Component | File | Complexity | Effort | Risk |
|-----------|------|-----------|--------|------|
| Four diagonal buttons (XAML) | HandController.xaml | LOW | 1-1.5 hours | LOW |
| SlewDirection enum additions | Enums.cs | VERY LOW | 0.5 hours | LOW |
| SkyTelescopeVm diagonal commands | SkyTelescopeVM.cs | LOW-MODERATE | 2-3 hours | LOW |
| HandControlVm diagonal commands | HandControlVM.cs | LOW-MODERATE | 2-3 hours | LOW |
| Stop-both-axes MouseUp logic | Both VMs | LOW | included above | LOW |
| Tooltip properties + resources | Both VMs + resources | VERY LOW | 0.5 hours | LOW |
| Testing & Validation | Various | LOW | 1-2 hours | LOW |

### Total Estimates

#### Gamepad + SkyServer only
- **Option A (Sequential Commands)**: 10-17 hours, Complexity: 3/10
- **Option B (New Diagonal Commands)**: 15-23 hours, Complexity: 5/10

#### Full stack (Gamepad + SkyServer + UI Buttons)
- **Option A (Sequential Commands)**: 17-26 hours, Complexity: 4/10
- **Option B (New Diagonal Commands)**: 22-32 hours, Complexity: 5/10

> **Note**: If the gamepad feature is implemented first, the incremental cost of adding UI diagonal buttons drops to approximately **6-8 hours** — the `SlewDirection` extension and `HcMoves` changes are shared and need not be repeated.

### Integration Points

#### 1. GamepadVM → HandControlVM
**Current Flow**:
```
Button Press → DoGamePadCommand("up") → HcMouseDownUpCommand → SkyServer.HcMoves(North)
```

**Proposed Flow (Option A)**:
```
Multiple Buttons → Aggregate Directions → 
  HcMouseDownUpCommand + HcMouseDownLeftCommand → 
  SkyServer.HcMoves([North, West])
```

#### 2. HandControlVM → SkyServer
**Current**: Single direction per call  
**Proposed**: Array of directions per call  
**Impact**: Minimal - existing command infrastructure handles this

#### 3. SkyServer → Mount Hardware
**Current**: Sets change[0] and change[1] independently  
**Proposed**: Same behavior, both axes can have simultaneous values  
**Impact**: None - already supported

---

## Testing Strategy

### Unit Testing Requirements

#### 1. Direction Aggregation Tests
```csharp
[Test]
public void WhenNorthAndWestPressed_ThenBothDirectionsAggregated() {
    var directions = new ActiveDirections { Up = true, Left = true };
    var result = directions.GetDirections();
    Assert.That(result, Contains.Item(SlewDirection.SlewUp));
    Assert.That(result, Contains.Item(SlewDirection.SlewLeft));
    Assert.That(directions.IsDiagonal, Is.True);
}
```

#### 2. State Transition Tests
Test all transition scenarios:
- None → Diagonal → None
- None → Single → Diagonal → Single → None
- Diagonal → Opposing Diagonal (NE → SW)

#### 3. Opposing Direction Cancellation
```csharp
[Test]
public void WhenOpposingDirectionsPressed_ThenAxisCancelled() {
    // User presses both Up and Down
    var directions = new ActiveDirections { Up = true, Down = true };
    // Should result in no vertical movement
}
```

#### 4. Partial Release Handling
```csharp
[Test]
public void WhenOneDirectionReleased_ThenTransitionsToSingleAxis() {
    // Start: NW diagonal
    // Action: Release West
    // Expected: Vertical movement only
}
```

### Integration Testing

#### 1. GamepadVM → HandControlVM Flow
- Verify correct commands triggered for each diagonal combination
- Confirm state tracking prevents repeat commands
- Test transition smoothness

#### 2. HandControlVM → SkyServer Flow
- Verify dual-axis rates calculated correctly
- Test all alignment modes (AltAz, Polar, German Polar)
- Confirm anti-backlash compensation applies correctly

#### 3. End-to-End Hardware Tests
- Test with actual gamepad on real mount
- Verify smooth diagonal motion at various speeds
- Confirm no "stuttering" during direction changes
- Test all 8 directional combinations: N, NE, E, SE, S, SW, W, NW

### Edge Cases to Test

| Scenario | Expected Behavior |
|----------|-------------------|
| Rapid button mashing | Smooth transitions, no command flooding |
| Holding diagonal then releasing one button | Graceful transition to single-axis |
| Pressing three buttons (e.g., N+E+W) | Cancel opposing (E+W), move North only |
| Speed change while moving diagonally | Both axes adjust speed simultaneously |
| Alignment mode switch during diagonal move | Stop movement, prevent mode conflict |
| Anti-backlash triggered during diagonal | Compensation applied per-axis independently |

---

## Risk Assessment

### Technical Risks

#### 1. Timing Issues
**Risk**: User presses buttons at slightly different times (within ~50ms)  
**Mitigation**: Implement small time window to accumulate directions  
**Severity**: LOW  
**Likelihood**: MEDIUM

#### 2. Button Release Order
**Risk**: Releasing one button before the other causes jarring transition  
**Mitigation**: Smooth rate transitions using existing state tracking  
**Severity**: LOW  
**Likelihood**: HIGH

#### 3. Anti-Backlash Interference
**Risk**: Diagonal moves with backlash compensation cause unexpected behavior  
**Mitigation**: Apply compensation per-axis as currently done  
**Severity**: LOW  
**Likelihood**: LOW

#### 4. Alt/Az Tracking Conflicts
**Risk**: Alt/Az tracking timer interferes with diagonal HC moves  
**Mitigation**: Acquire tracking lock during HC moves (existing pattern)  
**Severity**: MODERATE  
**Likelihood**: LOW

### Implementation Risks

#### 1. Breaking Existing Functionality
**Risk**: Changes to HcMoves break single-direction moves  
**Mitigation**: Comprehensive regression testing  
**Severity**: HIGH  
**Likelihood**: LOW

#### 2. Performance Degradation
**Risk**: Additional processing in polling loop slows response  
**Mitigation**: Profile before/after, optimize if needed  
**Severity**: LOW  
**Likelihood**: VERY LOW

#### 3. State Management Bugs
**Risk**: Complex state tracking introduces edge-case bugs  
**Mitigation**: Thorough state transition testing  
**Severity**: MODERATE  
**Likelihood**: MEDIUM

---

## Recommendations

### Recommended Implementation Path

#### Step 1: Prototype (2 hours)
1. Modify GamepadVM polling loop to accumulate directions
2. Log detected combinations without executing commands
3. Verify detection works correctly

#### Step 2: Core Gamepad Implementation (5 hours)
1. Implement `ActiveDirections` state tracker (2 hours)
2. Modify `DoGamePadCommand` to call multiple HC commands for diagonals (3 hours)

#### Step 2a: UI Diagonal Buttons — XAML (1-1.5 hours)
*Can be done in parallel with Step 2 or independently*
1. Add four diagonal buttons to `HandController.xaml` at grid corners `(0,0)`, `(0,2)`, `(2,0)`, `(2,2)`
2. Use `ArrowTopLeft/Right`, `ArrowBottomLeft/Right` icons from existing MaterialDesign package
3. Bind to new `HcMouseDown{NW/NE/SW/SE}Command` and `HcMouseUp{NW/NE/SW/SE}Command`

#### Step 3: Shared Layer — SlewDirection + SkyServer (2.5 hours)
*These changes serve both the gamepad and the UI button features*
1. Add `SlewNorthEast`, `SlewNorthWest`, `SlewSouthEast`, `SlewSouthWest` to `SlewDirection` enum (0.5 hours)
2. Modify `HcMoves` in `SkyServer.cs` to handle diagonal direction cases (2 hours)

#### Step 4: ViewModel Commands (4-6 hours)
1. Add eight diagonal commands to `SkyTelescopeVm` with combined flip logic and stop-both-axes MouseUp handling (2-3 hours)
2. Add identical eight commands to `HandControlVm` (2-3 hours)
3. Add `HcToolTip{NW/NE/SW/SE}` tooltip properties and resource strings (0.5 hours)

#### Step 5: Integration Testing (4 hours)
1. Unit tests for direction aggregation (1 hour)
2. Integration tests with gamepad (1.5 hours)
3. UI button tests — click/hold/release transitions (0.5 hours)
4. Hardware testing on real mount (1 hour)

#### Step 6: Refinement (4 hours)
1. Tune timing and transitions (2 hours)
2. Edge case handling — Pulse mode button visibility, opposing-direction cancellation (1 hour)
3. Documentation (1 hour)

#### Optional Step 7: Advanced Commands (6 hours)
*Only if Option B chosen*
1. Create diagonal HC commands in `HandControlVM` with dedicated `SlewDirection` values
2. Update `GamepadVM` to use new commands
3. Additional testing

### Development Best Practices

1. **Version Control**: Create feature branch `feature/diagonal-gamepad-motion`
2. **Incremental Commits**: Small, focused commits for each modification point
3. **Code Reviews**: Review changes at each phase before proceeding
4. **Testing First**: Write tests before implementation where possible
5. **Backward Compatibility**: Ensure single-direction moves still work throughout

### Configuration Considerations

Consider adding settings:
```csharp
// In SkySettings or GamepadSettings
public static int DiagonalDetectionWindowMs { get; set; } = 50;
public static bool EnableDiagonalMotion { get; set; } = true;
public static DiagonalMotionBehavior DiagonalBehavior { get; set; } = DiagonalMotionBehavior.Sequential;
```

---

## Appendix

### A. Code Snippets

#### Current HcMoves Switch Pattern
```csharp
switch (direction) {
    case SlewDirection.SlewNorth:
    case SlewDirection.SlewUp:
        change[1] = delta;
        break;
    case SlewDirection.SlewSouth:
    case SlewDirection.SlewDown:
        change[1] = -delta;
        break;
    case SlewDirection.SlewEast:
    case SlewDirection.SlewLeft:
        change[0] = SouthernHemisphere && !altAzModeSet ? -delta : delta;
        break;
    case SlewDirection.SlewWest:
    case SlewDirection.SlewRight:
        change[0] = SouthernHemisphere && !altAzModeSet ? delta : -delta;
        break;
}
```

#### Proposed Diagonal Support
```csharp
// Accumulate changes for all directions
foreach (var direction in directions) {
    switch (direction) {
        case SlewDirection.SlewNorth:
        case SlewDirection.SlewUp:
            change[1] += delta;
            break;
        case SlewDirection.SlewSouth:
        case SlewDirection.SlewDown:
            change[1] -= delta;
            break;
        case SlewDirection.SlewEast:
        case SlewDirection.SlewLeft:
            change[0] += SouthernHemisphere && !altAzModeSet ? -delta : delta;
            break;
        case SlewDirection.SlewWest:
        case SlewDirection.SlewRight:
            change[0] += SouthernHemisphere && !altAzModeSet ? delta : -delta;
            break;
    }
}

// Cancel opposing directions
if (HasOpposingDirections(directions)) {
    // Logic to cancel out opposing axes
}
```

### B. Directional Combinations Matrix

| Combination | Vertical Axis | Horizontal Axis | Expected Behavior |
|-------------|--------------|-----------------|-------------------|
| North | Y+ | 0 | Move up/north only |
| Northeast | Y+ | X+ | Move diagonally up-right |
| East | 0 | X+ | Move right/east only |
| Southeast | Y- | X+ | Move diagonally down-right |
| South | Y- | 0 | Move down/south only |
| Southwest | Y- | X- | Move diagonally down-left |
| West | 0 | X- | Move left/west only |
| Northwest | Y+ | X- | Move diagonally up-left |
| North+South | 0 | 0 | Cancel (no movement) |
| East+West | 0 | 0 | Cancel (no movement) |

### C. Alignment Mode Considerations

#### AltAz Mode
- Primary axis: Azimuth (X)
- Secondary axis: Altitude (Y)
- **Special Handling**: Alt/Az tracking timer must be managed during HC moves
- **Diagonal Support**: Full diagonal motion available

#### Polar Mode
- Primary axis: Right Ascension (X)
- Secondary axis: Declination (Y)
- **Direction Flip**: Left/Right OTA affects direction mapping
- **Diagonal Support**: Full diagonal motion available

#### German Polar Mode
- Primary axis: Hour Angle (X)
- Secondary axis: Declination (Y)
- **Side of Pier**: Affects direction calculations
- **Diagonal Support**: Full diagonal motion available with SoP awareness

### D. References

**Key Files**:
- `GS.Server\SkyTelescope\SkyServer.cs` — Mount control and HcMoves method
- `GS.Server\SkyTelescope\Enums.cs` — `SlewDirection`, `SlewSpeed`, `HcMode` enums
- `GS.Server\Gamepad\GamepadVM.cs` — Gamepad polling and command routing
- `GS.Server\Gamepad\IGamepad.cs` — Gamepad hardware interface
- `GS.Server\Gamepad\Gamepad.cs` — Base gamepad implementation
- `GS.Server\Controls\HandController.xaml` — Shared directional button UserControl (3×3 grid)
- `GS.Server\Controls\HandController.xaml.cs` — Code-behind (trivial — only `InitializeComponent`)
- `GS.Server\Windows\HandControlVM.cs` — Hand controller command definitions (floating window context)
- `GS.Server\Windows\HandControlV.xaml` — Floating HC window (`Width=340`, `Height=220`)
- `GS.Server\SkyTelescope\SkyTelescopeV.xaml` — Main window (embeds `HandController` UserControl at line 1195)
- `GS.Server\SkyTelescope\SkyTelescopeVM.cs` — Main ViewModel with HC command definitions and keyboard handler

**Related Enums**:
- `SlewDirection` — Direction enumeration (add `SlewNorthEast`, `SlewNorthWest`, `SlewSouthEast`, `SlewSouthWest`)
- `SlewSpeed` — Speed settings (One through Eight)
- `HcMode` — Hand controller mode (Axes, Guiding, Pulse)

**Related Classes**:
- `HcPrevMove` — Tracks previous move for anti-backlash
- `Vector` — 2D vector for X/Y values
- `SkyPredictor` — Tracking predictor for Alt/Az mode

---

### E. UI Layer Implementation Pattern

#### Diagonal Button XAML Template

Each diagonal button follows the same pattern as existing cardinal buttons. Example for NW corner `(Row 0, Col 0)`:

```xml
<Button Grid.Row="0" Grid.Column="0" Width="50" Height="50" ToolTipService.Placement="Center">
    <Button.ToolTip>
        <TextBlock Text="{Binding HcToolTipNW}" />
    </Button.ToolTip>
    <b:Interaction.Triggers>
        <b:EventTrigger EventName="PreviewMouseLeftButtonDown">
            <b:InvokeCommandAction Command="{Binding HcMouseDownNWCommand}" PassEventArgsToCommand="True" />
        </b:EventTrigger>
        <b:EventTrigger EventName="PreviewMouseLeftButtonUp">
            <b:InvokeCommandAction Command="{Binding HcMouseUpNWCommand}" PassEventArgsToCommand="True" />
        </b:EventTrigger>
    </b:Interaction.Triggers>
    <md:PackIcon Kind="ArrowTopLeft" />
</Button>
```

Grid positions for all four diagonals:

| Direction | Grid.Row | Grid.Column | Icon |
|-----------|----------|-------------|------|
| NW | 0 | 0 | `ArrowTopLeft` |
| NE | 0 | 2 | `ArrowTopRight` |
| SW | 2 | 0 | `ArrowBottomLeft` |
| SE | 2 | 2 | `ArrowBottomRight` |

#### Diagonal Command Handler Pattern

Each `HcMouseDown{Diagonal}` handler applies both flip corrections independently, then issues two `StartSlew` calls (Option A) or one combined call (Option B):

```csharp
// Option A — Two sequential StartSlew calls
private void HcMouseDownNW()
{
    if (SkyServer.AtPark) { /* ... blink parked ... */ return; }
    StartSlew(FlipNs && NsEnabled ? SlewDirection.SlewDown : SlewDirection.SlewUp);
    StartSlew(FlipEw && EwEnabled ? SlewDirection.SlewRight : SlewDirection.SlewLeft);
}

// Corresponding MouseUp — stops BOTH axes
private void HcMouseUpNW()
{
    StartSlew(SlewDirection.SlewNoneDec);
    StartSlew(SlewDirection.SlewNoneRa);
}
```

#### HcMode Pulse Consideration

Diagonal motion is undefined in `HcMode.Pulse`. The diagonal buttons can be conditionally hidden or disabled:

```xml
<!-- Optional: hide diagonal buttons in Pulse mode -->
<Button Grid.Row="0" Grid.Column="0" ...>
    <Button.Style>
        <Style TargetType="Button" BasedOn="{StaticResource MaterialDesignRaisedButton}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding HcMode}" Value="Pulse">
                    <Setter Property="Visibility" Value="Collapsed" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Button.Style>
    ...
</Button>
```

---

## Conclusion

The GSServer codebase is **architecturally ready** for diagonal motion across all three implementation layers with minimal changes required. The primary work involves **logical restructuring** of the input aggregation layer, mechanical command duplication across two ViewModels, and a small shared extension to `SlewDirection` and `SkyServer.HcMoves`.

**Key Success Factors**:
1. ✅ Hardware and interface layer already support multi-button detection
2. ✅ Mount command infrastructure already supports simultaneous dual-axis movement
3. ✅ Existing state tracking infrastructure can be extended for diagonal tracking
4. ✅ UI grid corners are physically empty — zero structural layout changes required
5. ✅ MaterialDesign icon set already covers diagonal arrow icons
6. ✅ No breaking changes to core mount control algorithms required
7. ✅ `SlewDirection` + `HcMoves` changes are shared across all three layers — implement once, benefit everywhere

**No architectural blockers exist in any layer.** This is a **straightforward enhancement** with well-defined modification points, low integration risk, and clear testing strategies.

**Recommended Timeline**:
- Gamepad + SkyServer only: 15-20 hours (Option A)
- Full stack including UI buttons: 22-28 hours (Option A)
- UI buttons incremental cost (after gamepad work): ~6-8 hours

---

**Document Version**: 1.1  
**Last Updated**: January 2025 — v1.1 adds UI Hand Controller Buttons layer analysis  
**Next Review**: After implementation completion

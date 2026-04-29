# Serial Hardware Disconnection Detection — Design Review
**Project:** GSServer (Principia4834/GSServer)  
**Date:** 2025  
**Scope:** USB-serial hardware disconnection detection and signalling strategy  
**Files analysed:** `GS.SkyWatcher\Commands.cs`, `GS.SkyWatcher\SkyWatcher.cs`, `GS.SkyWatcher\SkyQueue.cs`, `GS.SkyWatcher\SharedResources.cs`, `GS.Server\SkyTelescope\SkyServer.cs`

---

## 1. Problem Statement

When a USB-serial connection to the SkyWatcher mount is physically removed (cable pull, power loss, USB hub failure), the application does not reliably detect the hardware disconnection in a timely or deterministic way. The consequence is that the mount server continues to operate as if connected, with silent failures and eventual undefined behaviour.

Three complementary approaches were explored:

1. OS-level event notification (passive, background detection)
2. IO exception classification at the serial operation level (active, at time of failure)
3. Win32 handle probing (deterministic hardware-gone confirmation)

---

## 2. Codebase Architecture — Call Chain

```
ASCOM Client
  └── Telescope.cs  (ASCOM driver layer)
        └── SkyServer.cs  (mount server, SkyErrorHandler)
              └── SkyQueue.cs  (BlockingCollection command queue)
                    └── SkyWatcher.cs  (mid-layer, delegates all calls)
                          └── Commands.cs  ← ONLY class touching serial port
                                └── SkyQueue.Serial (ISerialPort)
                                      └── SerialPort (System.IO.Ports)
                                            └── Win32 HANDLE (usbser.sys)
```

**Key finding:** `Commands.CmdToMount()` is the single chokepoint for ALL serial IO to both axes. It is the optimum detection point.

---

## 3. Current Behaviour Analysis

### 3.1 `Commands.CmdToMount()` retry loop
- Outer retry: 0–5 attempts (`ConErrMax = 5`)
- Inner retry: 0–10 attempts
- On `IOException` → sets `MountConnected = false`, throws `MountControlException(ErrorCode.ErrNotConnected)`
- On `TimeoutException` → throws `MountControlException(ErrorCode.ErrNoResponseAxis1/2)`
- Total latency before escalation: up to ~500ms

### 3.2 Write-side failure (detectable)
`SendRequest()` calls `SkyQueue.Serial.Write()`. When hardware is gone, `Write()` throws `IOException`. This propagates up through the retry loop correctly.

### 3.3 Read-side failure (SILENT — critical gap)
`ReceiveResponse()` calls `SkyQueue.Serial.ReadExisting()`. When hardware is gone, `ReadExisting()` returns **an empty string** — no exception is thrown. The current code treats this as a normal empty response, silently failing.

### 3.4 `SerialPort.IsOpen` is unreliable
After USB removal, `IsOpen` returns `true` until an IO operation is attempted. The `IsConnected` property (`IsOpen AND MountConnected`) is therefore unreliable immediately after disconnection.

### 3.5 `ProcessCommandQueue()` swallows exceptions
In `SkyQueue.cs`, the command processing loop catches all exceptions into `command.Exception` and sets `command.Successful = false`. Exceptions never propagate to force queue shutdown.

### 3.6 `SkyErrorHandler()` treats all errors identically
In `SkyServer.cs`, all `ErrorCode` values result in `IsMountRunning = false` and `MountError = ex`. There is no differentiation between transient errors and permanent hardware disconnection.

### 3.7 `SerialPort.Close()` deadlock risk
A known .NET Framework issue: calling `SerialPort.Close()` after USB removal can deadlock indefinitely. This must be handled with a timeout wrapper or a separate thread.

---

## 4. Four Optimum Detection/Signalling Points

### Point A — `Commands.CmdToMount()` (Detection)
The IO chokepoint. All serial failures must be classified here.
- Catch `IOException` and inspect `InnerException as Win32Exception`
- Use `NativeErrorCode` to classify hardware-gone vs transient
- After consecutive empty responses from `ReadExisting()`, use Win32 `GetCommState` probe to confirm hardware gone

### Point B — `ErrorCode` enum (Classification)
Located in `GS.SkyWatcher\SharedResources.cs`. Currently has no hardware-disconnect-specific value.
- Add: `ErrHardwareDisconnected = 502` (or similar)
- This carries the classification upward through the exception chain

### Point C — `ProcessCommandQueue()` in `SkyQueue.cs` (Propagation)
Currently swallows all exceptions. For `ErrHardwareDisconnected`:
- Stop accepting new commands (mark queue as faulted)
- Signal callers immediately rather than waiting for queue drain

### Point D — `SkyErrorHandler()` in `SkyServer.cs` (Response)
Currently treats all errors identically. Should differentiate:
- `ErrHardwareDisconnected` → immediate clean shutdown, user notification, no retry
- Other errors → existing behaviour (may retry, log, etc.)

---

## 5. OS-Level Detection Approaches

These run in the background independently of IO operations and can give early warning:

### 5.1 `ManagementEventWatcher` (WMI) — Recommended for background detection
```csharp
var query = new WqlEventQuery(
    "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");
var watcher = new ManagementEventWatcher(query);
watcher.EventArrived += (s, e) => { /* device removed */ };
watcher.Start();
```
- Fires ~500ms after physical removal (registry update latency)
- Good for UI notification, not for immediate IO protection

### 5.2 `WM_DEVICECHANGE` / `DBT_DEVICEREMOVECOMPLETE` (Win32)
- Requires a window handle (suitable since this is a WPF application)
- Fires faster than WMI (~100–200ms)
- Most reliable OS-level notification

### 5.3 `SerialPort.ErrorReceived`
- Unreliable — does not fire consistently on USB removal
- Driver-dependent behaviour
- Not recommended as primary detection

### 5.4 `SerialPort.PinChanged`
- Fires when DSR/CTS/DCD lines change
- Only works if the USB-serial adapter asserts modem control lines
- SkyWatcher mounts do not reliably do this

---

## 6. Win32 Handle Deep Dive

### 6.1 What the handle is
When `SerialPort` opens a COM port, it calls:
```c
HANDLE hCom = CreateFile("\\\\.\\COM3", GENERIC_READ | GENERIC_WRITE,
    0, NULL, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, NULL);
```
This Win32 `HANDLE` is the kernel object token for the serial device.

### 6.2 Access chain in .NET
```
SerialPort
  └── .BaseStream          → Stream (FileStream internally)
        └── (FileStream).SafeFileHandle  → SafeFileHandle
              └── .DangerousGetHandle()  → IntPtr (raw Win32 HANDLE)
```
> Note: `SerialPort.BaseStream` is typed as `Stream`, not `FileStream`. A cast is required to access `SafeFileHandle`.

### 6.3 `SafeFileHandle.IsInvalid` — NOT useful for disconnect detection
- Returns `true` only when handle value is 0 or -1 (`INVALID_HANDLE_VALUE`)
- After USB removal, the kernel object stays alive (handle stays non-invalid)
- `IsInvalid` remains `false` — same reason `SerialPort.IsOpen` lies

### 6.4 `GetCommState` P/Invoke — The reliable hardware probe
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool GetCommState(IntPtr hFile, ref DCB lpDCB);
```
- Calls into the driver's `DeviceControl` IRP handler
- **Device present:** returns `true`
- **Device gone:** returns `false`, `Marshal.GetLastWin32Error()` returns:
  - `2` — `ERROR_FILE_NOT_FOUND`
  - `5` — `ERROR_ACCESS_DENIED`
  - `22` — `ERROR_BAD_COMMAND`
  - `31` — `ERROR_GEN_FAILURE` ← most common on USB serial removal
- Cost: ~1 µs kernel call (non-blocking, synchronous)

### 6.5 `ClearCommError` P/Invoke — Additional information
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool ClearCommError(IntPtr hFile, out uint lpErrors, out COMSTAT lpStat);
```
- Returns hardware error bitmask: `CE_IOE (0x0400)` = hardware I/O error
- Returns bytes in receive buffer (`cbInQue`)
- Same return behaviour as `GetCommState` on hardware gone

### 6.6 Detection method comparison

| Method | Detects hardware gone | When | Overhead |
|---|---|---|---|
| `SerialPort.IsOpen` | **Never** (lies) | N/A | Zero |
| `SafeFileHandle.IsInvalid` | **Never** (handle stays valid) | N/A | Zero |
| `GetCommState()` P/Invoke | **Yes** | Immediately after blind window | ~1 µs |
| `GetPortNames()` | **Yes** | ~500ms after removal | Registry scan |
| `IOException` on Write | **Yes** | On next write attempt | Retry latency |
| `ReadExisting()` empty | **Never** without extra check | N/A | Zero (silent) |

---

## 7. USB Driver Stack Blind Window

After physical USB removal, there is a timing delay before Win32 calls return accurate state:

```
Physical USB disconnect
  └── USB hub detects VBUS loss          ~  8–16ms  (one USB poll interval)
        └── USB device driver notified   ~  1–5ms
              └── usbser.sys IRP cancel  ~  5–50ms
                    └── Win32 HANDLE invalidated  ← GetCommState now fails
```

**Total blind window: ~15–100ms** (up to 500ms on some systems/drivers)

### Per-driver behaviour during blind window

| Driver | Behaviour |
|---|---|
| `usbser.sys` (generic) | May return `true` for 1–3 poll intervals |
| FTDI virtual COM | Returns `ERROR_GEN_FAILURE` quickly (~20ms) |
| Prolific PL2303 | May hang up to 300ms before returning error |
| Silicon Labs CP210x | Generally fast (~30ms) |

### Implication
`GetCommState` is **not a real-time detector** — it is a **confirmer** used after the serial protocol has already indicated something is wrong. By the time the retry loop has run 2–3 times (30ms minimum at 10ms sleep), the blind window has naturally elapsed.

---

## 8. Win32 Error Code Classification

For `IOException.InnerException as Win32Exception`, the `NativeErrorCode` values that indicate hardware gone:

| Code | Constant | Meaning |
|---|---|---|
| `2` | `ERROR_FILE_NOT_FOUND` | Port no longer exists |
| `5` | `ERROR_ACCESS_DENIED` | Port locked by driver teardown |
| `22` | `ERROR_BAD_COMMAND` | Driver not accepting commands |
| `31` | `ERROR_GEN_FAILURE` | Hardware failure (most common) |

Other codes (e.g., `6` = `ERROR_INVALID_HANDLE`, `1167` = `ERROR_DEVICE_NOT_CONNECTED`) may also appear on some hardware.

---

## 9. Proposed Detection Pattern for `Commands.cs`

### 9.1 Win32 probe helper
```csharp
[DllImport("kernel32.dll", SetLastError = true)]
private static extern bool GetCommState(IntPtr hFile, ref DCB lpDCB);

[StructLayout(LayoutKind.Sequential)]
private struct DCB
{
    public uint DCBlength;
    public uint BaudRate;
    [MarshalAs(UnmanagedType.U4)] public uint fFlags;
    public ushort wReserved;
    public ushort XonLim;
    public ushort XoffLim;
    public byte ByteSize;
    public byte Parity;
    public byte StopBits;
    public byte XonChar;
    public byte XoffChar;
    public byte ErrorChar;
    public byte EofChar;
    public byte EvtChar;
    public ushort wReserved1;
}

private bool IsSerialHandleAlive()
{
    try
    {
        var stream = SkyQueue.Serial?.BaseStream as System.IO.FileStream;
        if (stream == null || stream.SafeFileHandle.IsInvalid || stream.SafeFileHandle.IsClosed)
            return false;
        var dcb = new DCB { DCBlength = (uint)Marshal.SizeOf<DCB>() };
        return GetCommState(stream.SafeFileHandle.DangerousGetHandle(), ref dcb);
    }
    catch { return false; }
}
```

### 9.2 Threshold-based confirmation (in `CmdToMount` retry loop)
```csharp
private int _consecutiveEmptyResponses = 0;
private const int HardwareGoneThreshold = 3;

// After ReadExisting() returns empty string:
_consecutiveEmptyResponses++;
if (_consecutiveEmptyResponses >= HardwareGoneThreshold)
{
    if (!IsSerialHandleAlive())
    {
        throw new MountControlException(ErrorCode.ErrHardwareDisconnected,
            "Hardware disconnected: serial handle no longer alive");
    }
}
// Reset on successful response:
_consecutiveEmptyResponses = 0;
```

### 9.3 IOException classification
```csharp
catch (IOException ex)
{
    var win32 = ex.InnerException as System.ComponentModel.Win32Exception;
    if (win32 != null)
    {
        switch (win32.NativeErrorCode)
        {
            case 2:   // ERROR_FILE_NOT_FOUND
            case 5:   // ERROR_ACCESS_DENIED
            case 22:  // ERROR_BAD_COMMAND
            case 31:  // ERROR_GEN_FAILURE
                throw new MountControlException(ErrorCode.ErrHardwareDisconnected,
                    $"Hardware disconnected (Win32:{win32.NativeErrorCode})", ex);
        }
    }
    // Existing handling for other IOExceptions
    MountConnected = false;
    throw new MountControlException(ErrorCode.ErrNotConnected, ex.Message, ex);
}
```

---

## 10. `ErrorCode` Enum Change Required

In `GS.SkyWatcher\SharedResources.cs`:
```csharp
public enum ErrorCode
{
    // ... existing values ...
    ErrNotConnected     = 3,
    ErrNoResponseAxis1  = 100,
    ErrNoResponseAxis2  = 101,
    ErrTooManyRetries   = 501,
    ErrHardwareDisconnected = 502  // NEW: permanent hardware loss
}
```

---

## 11. `SerialPort.Close()` Deadlock Avoidance

A known .NET Framework issue: `SerialPort.Close()` can deadlock after USB removal because the internal read thread may be blocked waiting for data that will never arrive.

**Safe close pattern:**
```csharp
private void SafeCloseSerial(SerialPort port)
{
    if (port == null) return;
    port.DtrEnable = false;  // release modem lines first
    var closeThread = new Thread(() => { try { port.Close(); } catch { } });
    closeThread.IsBackground = true;
    closeThread.Start();
    if (!closeThread.Join(2000))  // 2 second timeout
    {
        // Thread is stuck - abandon it, GC will eventually clean up
        // Log warning here
    }
}
```

---

## 12. Summary of Recommended Changes

| File | Change | Priority |
|---|---|---|
| `SharedResources.cs` | Add `ErrHardwareDisconnected = 502` to `ErrorCode` enum | High |
| `Commands.cs` | Add `IsSerialHandleAlive()` using `GetCommState` P/Invoke | High |
| `Commands.cs` | Add `_consecutiveEmptyResponses` counter; probe after threshold | High |
| `Commands.cs` | Classify `Win32Exception.NativeErrorCode` in `IOException` handler | High |
| `Commands.cs` | Add safe `SerialPort.Close()` with thread timeout | Medium |
| `SkyQueue.cs` | On `ErrHardwareDisconnected`: stop queue, signal immediately | Medium |
| `SkyServer.cs` | In `SkyErrorHandler`: differentiate `ErrHardwareDisconnected` from transient errors | Medium |
| `SkyServer.cs` (or WPF layer) | Add `ManagementEventWatcher` or `WM_DEVICECHANGE` for background notification | Low |

---

## 13. Key Risks and Constraints

- **C# 7.3 / .NET Framework 4.7.2** — no `IAsyncDisposable`, no `System.IO.Ports` improvements from .NET 6+. All patterns must use framework-compatible APIs.
- **`DangerousGetHandle()`** requires care — must not be called after handle is closed. Always check `IsClosed` and `IsInvalid` first.
- **Driver-specific behaviour** — the blind window and error codes vary by USB-serial chip manufacturer. The threshold pattern (3 consecutive failures before probing) mitigates this.
- **PHD2 2-second timeout** — the guiding software expects `PulseGuide` to return within 2 seconds. Any disconnection handling must not block the ASCOM driver thread for longer than this.
- **`ProcessCommandQueue` runs on a background Task** — `ErrHardwareDisconnected` must be able to signal back to the UI thread safely (existing `MountError` property and `SkyErrorHandler` mechanism can carry this).

---

*End of document*

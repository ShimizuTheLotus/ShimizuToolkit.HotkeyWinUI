# ShimizuToolkit.HotkeyWinUI

A easy way to manage global hotkeys (keyboard accelerators) in WinUI.

## Why ShimizuToolkit.HotkeyWinUI?

- It could work on different CPU, x86, x64 and AMD64 CPU, designed for WinUI platform.
- It could distinguish left and right modifiers, which can give you more choices on hotkeys.

## Usage

```csharp
// Define hotkey
HotKeyInfo hotKeyInfo = new("OpenWindow", OpenWindow, new List<VirtualKey>(){ VirtualKey.VK_LSHIFT, VirtualKey.VK_LCONTROL, VirtualKey.VK_P}, true);

private void OpenWindow(object sender, ShimizuToolkit.HotkeyWinUI.HotKeyEventArgs e){}
```
Parameters:

`"OpenWindow"`: Identifier, you can use it to register, replace or delete hotkeys from hotkey manager.

`OpenWindow`: the event to call when the hotkey is called.

`new List<VirtualKey>(){ VirtualKey.VK_LSHIFT, VirtualKey.VK_LCONTROL, VirtualKey.VK_P}`: keys in hotkey. it could have multiple modifier keys(L/R Shift, Control, Win, Alt). And should only have one action key(the key that is not a modifier key).

`true`: Ignore L/R of modifier keys in the hotkey. Default value is `true`. here's a table to help you understand.

We use hotkey LShift + LWin + D as an example

| key input | `true` behavior | `false` behavior |
| --- | --- | --- | --- |
| LShift + LWin + D | trigger | trigger |
| LShift + RWin + D | trigger | none |
| RShift + LWin + D | trigger | none |
| RShift + RWin + D | trigger | none |

`true` makes the hotkey works just like other hotkey libs.

`false` can enable specific detect for L/R modifier keys.`

```csharp
// Register or overwrite a hotkey
ShimizuToolkit.HotkeyWinUI.HotKeyManager.Current.AddOrOverwriteHotKey("OpenWindow", hotKeyInfo);
ShimizuToolkit.HotkeyWinUI.HotKeyManager.Current.AddOrOverwriteHotKey(hotKeyInfo);
```
"OpenWindow" is the identifier we just used. If you've already set an identifier you want to use, you can choose the second function. If there's an hotkey registered with a same identifier, it will be replaced.

```csharp
// Disable or enable hotkeys
hotKeyInfo.IsEnabled = true;
hotKeyInfo.IsEnabled = false;
```

```csharp
// Unregister a hotkey
ShimizuToolkit.HotkeyWinUI.HotKeyManager.Current.RemoveHotKey("OpenWindow");
```

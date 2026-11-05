# Controls the Unity editor through Windows UI Automation: screenshots, clicks, typing and key presses.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ui_automation.ps1 -Action window -Title "Unity"
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ui_automation.ps1 -Action shot -Out C:\tmp\shot.png
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ui_automation.ps1 -Action click -X 400 -Y 320
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ui_automation.ps1 -Action keys -Text "{ENTER}"
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ui_automation.ps1 -Action press -Key "RETURN"
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ui_automation.ps1 -Action type -Text "flap=6.2"
#   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/ui_automation.ps1 -Action tree -Title "Unity"
#
# -Title accepts part of the window title (case-insensitive). Coordinates are relative to the window unless
# -ScreenCoords is passed. Needs an interactive desktop session (it does not work from a service session).
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("shot", "click", "keys", "type", "press", "window", "tree", "focus", "find")]
    [string]$Action,
    [string]$Title = "Unity",
    [string]$Out = "",
    [int]$X = -1,
    [int]$Y = -1,
    [string]$Text = "",
    [string]$Key = "",
    [string]$Name = "",
    [switch]$ScreenCoords,
    [int]$Depth = 4,
    [int]$WindowLeft = 0,
    [int]$WindowTop = 0
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms, System.Drawing, UIAutomationClient, UIAutomationTypes

$source = @"
using System;
using System.Runtime.InteropServices;
public static class FcWin {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@
Add-Type -TypeDefinition $source
[FcWin]::SetProcessDPIAware() | Out-Null

function Find-Window([string]$part) {
    $procs = Get-Process | Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -like "*$part*" }
    if (-not $procs) {
        throw "No window whose title contains '$part'."
    }

    return $procs | Sort-Object Id -Descending | Select-Object -First 1
}

function Get-WindowRect($proc) {
    $rect = New-Object FcWin+RECT
    [FcWin]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    return $rect
}

function Focus-Window($proc) {
    [FcWin]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
    [FcWin]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
    Start-Sleep -Milliseconds 400
}

function Find-Element($root, [string]$name) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

switch ($Action) {
    "window" {
        $proc = Find-Window $Title
        $rect = Get-WindowRect $proc
        [pscustomobject]@{
            Id = $proc.Id
            Title = $proc.MainWindowTitle
            Handle = $proc.MainWindowHandle.ToInt64()
            Left = $rect.Left
            Top = $rect.Top
            Width = $rect.Right - $rect.Left
            Height = $rect.Bottom - $rect.Top
        } | ConvertTo-Json
    }
    "find" {
        $proc = Find-Window ""
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
        $element = Find-Element $root $Name
        if ($element -eq $null) {
            Write-Output "not found: $Name"
            exit 1
        }

        $element.Current.BoundingRectangle | Format-List | Out-String
        try {
            $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
            Write-Output "invoked: $Name"
        }
        catch {
            Write-Output "not invokable: " + $_.Exception.Message
        }
    }
    "focus" {
        $proc = Find-Window $Title
        Focus-Window $proc
        Write-Output "focused $($proc.Id)"
    }
    "shot" {
        $proc = Find-Window $Title
        Focus-Window $proc
        $rect = Get-WindowRect $proc
        $w = $rect.Right - $rect.Left
        $h = $rect.Bottom - $rect.Top
        $bitmap = New-Object System.Drawing.Bitmap $w, $h
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
        if (-not $Out) { $Out = Join-Path $env:TEMP "shot.png" }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
        $bitmap.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
        $graphics.Dispose()
        $bitmap.Dispose()
        Write-Output "saved $Out ($w x $h)"
    }
    "click" {
        $proc = Find-Window $Title
        Focus-Window $proc
        $rect = Get-WindowRect $proc
        $px = if ($ScreenCoords) { $X } else { $rect.Left + $X }
        $py = if ($ScreenCoords) { $Y } else { $rect.Top + $Y }
        [FcWin]::SetCursorPos($px, $py) | Out-Null
        Start-Sleep -Milliseconds 120
        [FcWin]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 90
        [FcWin]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
        Write-Output "clicked $px,$py"
    }
    "keys" {
        $proc = Find-Window $Title
        Focus-Window $proc
        [System.Windows.Forms.SendKeys]::SendWait($Text)
        Write-Output "sent keys"
    }
    "type" {
        $proc = Find-Window $Title
        Focus-Window $proc
        foreach ($ch in $Text.ToCharArray()) {
            $special = @('{', '}', '+', '^', '%', '~', '(', ')', '[', ']')
            $send = if ($special -contains [string]$ch) { "{" + $ch + "}" } else { [string]$ch }
            [System.Windows.Forms.SendKeys]::SendWait($send)
            Start-Sleep -Milliseconds 25
        }
        Write-Output "typed $($Text.Length) characters"
    }
    "press" {
        $proc = Find-Window $Title
        Focus-Window $proc
        [System.Windows.Forms.SendKeys]::SendWait("{" + $Key + "}")
        Write-Output "pressed $Key"
    }
    "tree" {
        $proc = Find-Window $Title
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($proc.MainWindowHandle)
        $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
        function Walk($element, $level) {
            if ($level -gt $Depth) { return }
            $child = $walker.GetFirstChild($element)
            while ($child -ne $null) {
                $name = $child.Current.Name
                $type = $child.Current.ControlType.ProgrammaticName -replace "ControlType.", ""
                if ($name -or $type -match "Button|Edit|Tab|Tree|List") {
                    Write-Output (("  " * $level) + "$type  '$name'")
                }
                Walk $child ($level + 1)
                $child = $walker.GetNextSibling($child)
            }
        }
        Walk $root 0
    }
}

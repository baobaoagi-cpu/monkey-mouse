# Review-only collector. Not executed by Codex here. No settings, registry, firewall or input changes.
# Run in ordinary Windows PowerShell. Output is preliminary until DPI coordinate space is reconciled.
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class HydraDisplayReadOnly {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
  [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] public struct MONITORINFOEX {
    public int Size; public RECT Monitor; public RECT Work; public uint Flags;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string Device;
  }
  public sealed class Reading {
    public string OutputName; public string VolatileMonitorHandle; public string AssignedRole;
    public int X,Y,Width,Height; public uint DpiX,DpiY; public int DpiStatus;
    public bool Primary;
  }
  public delegate bool Callback(IntPtr h, IntPtr dc, ref RECT clip, IntPtr data);
  [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,Callback cb,IntPtr data);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfoW(IntPtr h, ref MONITORINFOEX info);
  [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr h,int kind,out uint x,out uint y);
  public static List<Reading> Read() {
    var result=new List<Reading>();
    Callback cb=delegate(IntPtr h,IntPtr dc,ref RECT clip,IntPtr data) {
      var m=new MONITORINFOEX();m.Size=Marshal.SizeOf(typeof(MONITORINFOEX));
      if(!GetMonitorInfoW(h,ref m))return true;
      uint x,y; int status=GetDpiForMonitor(h,0,out x,out y);
      result.Add(new Reading {OutputName=m.Device,VolatileMonitorHandle=h.ToString(),AssignedRole=null,
        X=m.Monitor.L,Y=m.Monitor.T,Width=m.Monitor.R-m.Monitor.L,Height=m.Monitor.B-m.Monitor.T,
        Primary=(m.Flags&1)!=0,DpiX=x,DpiY=y,DpiStatus=status});return true;
    };
    if(!EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,cb,IntPtr.Zero))throw new Exception("Monitor enumeration failed");
    return result;
  }
}
'@
[ordered]@{
    collectedAt = (Get-Date).ToString('o')
    coordinateSpace = 'UNCONFIRMED: caller DPI-awareness affects these APIs'
    monitors = [HydraDisplayReadOnly]::Read()
    notes = @(
        'Assign WindowsExternal and WindowsLaptop after checking actual output/model; Primary is not a laptop identifier.',
        'HMONITOR is volatile across reconnect/reboot. Prefer confirmed OutputName/ScreenInfo identity.',
        'Do not convert DPI to Hydra MouseScale automatically; reconcile physical vs logical bounds first.',
        'No settings were changed. No Hydra or ShareMouse process was started or modified.'
    )
} | ConvertTo-Json -Depth 6

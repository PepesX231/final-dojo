using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace MamasComing.EditorTools
{
    /// <summary>Reports system RAM / commit usage (Windows). Used to keep an eye on memory while building.</summary>
    public static class MamaSysInfo
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static string Report()
        {
            var m = new MEMORYSTATUSEX();
            if (!GlobalMemoryStatusEx(m)) return "unavailable";
            const double G = 1024.0 * 1024 * 1024;
            double usedPhys = (m.ullTotalPhys - m.ullAvailPhys) / G, totPhys = m.ullTotalPhys / G;
            double usedCommit = (m.ullTotalPageFile - m.ullAvailPageFile) / G, totCommit = m.ullTotalPageFile / G;
            long unityMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024);
            return string.Format("RAM {0:0.0}/{1:0.0} GB ({2}%) | Commit {3:0.0}/{4:0.0} GB | Unity reserved {5} MB",
                usedPhys, totPhys, m.dwMemoryLoad, usedCommit, totCommit, unityMB);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PMC { public uint cb, PageFaultCount; public ulong PeakWorkingSetSize, WorkingSetSize, QuotaPeakPagedPoolUsage, QuotaPagedPoolUsage, QuotaPeakNonPagedPoolUsage, QuotaNonPagedPoolUsage, PagefileUsage, PeakPagefileUsage; }
        [DllImport("kernel32.dll")] static extern System.IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(System.IntPtr h);
        [DllImport("kernel32.dll")] static extern bool K32GetProcessMemoryInfo(System.IntPtr h, out PMC c, uint cb);

        [DllImport("user32.dll")] static extern bool ShowWindow(System.IntPtr h, int cmd);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(System.IntPtr h);
        [DllImport("user32.dll")] static extern System.IntPtr GetActiveWindow();

        /// <summary>Restore + bring this Unity editor window to the front.</summary>
        public static bool BringToFront()
        {
            var h = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (h == System.IntPtr.Zero) h = GetActiveWindow();
            ShowWindow(h, 3);
            return SetForegroundWindow(h);
        }

        [DllImport("user32.dll")] static extern bool MoveWindow(System.IntPtr h, int x, int y, int w, int hgt, bool repaint);
        [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);

        /// <summary>Restore and place the editor window at a pixel rect (fraction of screen width).</summary>
        public static string PlaceWindow(float widthFrac)
        {
            var h = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            int sw = GetSystemMetrics(0), sh = GetSystemMetrics(1);
            ShowWindow(h, 9);
            MoveWindow(h, 0, 0, (int)(sw * widthFrac), sh - 60, true);
            SetForegroundWindow(h);
            return sw + "x" + sh;
        }

        /// <summary>Top memory users grouped by process name (read-only).</summary>
        public static string TopProcesses(int n)
        {
            var ws = new System.Collections.Generic.Dictionary<string, ulong>();
            var cm = new System.Collections.Generic.Dictionary<string, ulong>();
            var ct = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var p in System.Diagnostics.Process.GetProcesses())
            {
                string name; try { name = p.ProcessName; } catch { continue; }
                var h = OpenProcess(0x1000 | 0x0010, false, p.Id);
                if (h == System.IntPtr.Zero) continue;
                PMC c;
                if (K32GetProcessMemoryInfo(h, out c, (uint)Marshal.SizeOf(typeof(PMC))))
                {
                    if (!ws.ContainsKey(name)) { ws[name] = 0; cm[name] = 0; ct[name] = 0; }
                    ws[name] += c.WorkingSetSize; cm[name] += c.PagefileUsage; ct[name]++;
                }
                CloseHandle(h);
            }
            var l = new System.Collections.Generic.List<string>(ws.Keys);
            l.Sort((a, b) => cm[b].CompareTo(cm[a]));
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < l.Count && i < n; i++)
                sb.AppendLine(l[i] + " x" + ct[l[i]] + "  commit " + (cm[l[i]] >> 20) + " MB  ram " + (ws[l[i]] >> 20) + " MB");
            return sb.ToString();
        }

        [MenuItem("Mama's Coming/Free Editor Memory")]
        public static void Free()
        {
            EditorUtility.UnloadUnusedAssetsImmediate(true);
            System.GC.Collect();
            Debug.Log("[Mama's Coming] " + Report());
        }
    }
}

using System.Runtime.InteropServices;

namespace Runner;

public record PowerPlan(string Name, Guid Guid);

class PowerManager
{
    /// <summary>
    /// Indicates that almost no power savings measures will be used.
    /// </summary>
    public PowerPlan MaximumPerformance { get; }

    /// <summary>
    /// Indicates that fairly aggressive power savings measures will be used.
    /// </summary>
    public PowerPlan Balanced { get; }

    /// <summary>
    /// Indicates that very aggressive power savings measures will be used to help
    /// stretch battery life.                                                     
    /// </summary>
    public PowerPlan PowerSourceOptimized { get; }

    public PowerManager()
    {

        // See GUID values in WinNT.h.
        MaximumPerformance = NewPlan("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        Balanced = NewPlan("381b4222-f694-41f0-9685-ff5bb260df2e");
        PowerSourceOptimized = NewPlan("a1841308-3541-4fab-bc81-f71556f20b4a");
    }

    private PowerPlan NewPlan(string guidString)
    {
        Guid guid = new Guid(guidString);
        return new PowerPlan(GetPowerPlanName(guid), guid);
    }

    public void SetActive(PowerPlan plan)
    {
        Guid planGuid = plan.Guid;
        PowerSetActiveScheme(IntPtr.Zero, ref planGuid);

        Console.WriteLine("Switched to " + plan.Name);
    }

    /// <returns>
    /// All supported power plans.
    /// </returns>
    public List<PowerPlan> GetPlans()
    {
        return
        [
            MaximumPerformance, Balanced, PowerSourceOptimized
        ];
    }

    private Guid GetActiveGuid()
    {
        Guid ActiveScheme = Guid.Empty;
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));
        if (PowerGetActiveScheme((IntPtr) null, out ptr) == 0)
        {
            var tempStruct = Marshal.PtrToStructure(ptr, typeof(Guid));
            if (tempStruct is not null)
            {
                ActiveScheme = (Guid)tempStruct;
            }
        }
        Marshal.FreeHGlobal(ptr);

        return ActiveScheme;
    }

    public PowerPlan GetCurrentPlan()
    {          
        Guid guid = GetActiveGuid();
        return GetPlans().First(p => p.Guid == guid);
    }

    private static string GetPowerPlanName(Guid guid)
    {
        string name = string.Empty;
        IntPtr lpszName = IntPtr.Zero;
        uint dwSize = 0;

        PowerReadFriendlyName((IntPtr) null, ref guid, (IntPtr) null, (IntPtr) null, lpszName, ref dwSize);
        if (dwSize > 0)
        {
            lpszName = Marshal.AllocHGlobal((int)dwSize);
            if (PowerReadFriendlyName((IntPtr)null, ref guid, (IntPtr)null, (IntPtr)null, lpszName, ref dwSize) == 0)
            {
                var tempName = Marshal.PtrToStringUni(lpszName);
                if (tempName is not null)
                {
                    name = tempName;
                }
            }

            if (lpszName != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(lpszName);
            }
        }

        return name;
    }

    #region DLL imports

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern int GetSystemDefaultLCID();

    [DllImport("powrprof.dll", EntryPoint = "PowerSetActiveScheme")]
    public static extern uint PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid ActivePolicyGuid);

    [DllImport("powrprof.dll", EntryPoint = "PowerGetActiveScheme")]
    public static extern uint PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

    [DllImport("powrprof.dll", EntryPoint = "PowerReadFriendlyName")]
    public static extern uint PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid, IntPtr PowerSettingGuid, IntPtr Buffer, ref uint BufferSize);

    #endregion
}
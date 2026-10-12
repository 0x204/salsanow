using System.Runtime.InteropServices;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Nvidia;

internal static class NvidiaPowerMode
{
    private const uint PowerManagementModeSetting = 0x1057EB71;
    private const uint PreferMaximumPerformance = 0x00000001;

    public static void Apply()
    {
        NvApi.Check(NvApi.Initialize(), "NvAPI_Initialize");

        IntPtr session = IntPtr.Zero;
        try
        {
            NvApi.Check(NvApi.CreateSession(out session), "NvAPI_DRS_CreateSession");
            NvApi.Check(NvApi.LoadSettings(session), "NvAPI_DRS_LoadSettings");

            NvApi.Check(NvApi.RestoreAllDefaults(session), "NvAPI_DRS_RestoreAllDefaults");

            NvApi.Check(NvApi.GetBaseProfile(session, out IntPtr baseProfile), "NvAPI_DRS_GetBaseProfile");

            IntPtr setting = NvApi.AllocateDwordSetting(PowerManagementModeSetting, PreferMaximumPerformance);
            try
            {
                NvApi.Check(NvApi.SetSetting(session, baseProfile, setting), "NvAPI_DRS_SetSetting (PowerMgmt)");
            }
            finally
            {
                Marshal.FreeHGlobal(setting);
            }

            NvApi.Check(NvApi.SaveSettings(session), "NvAPI_DRS_SaveSettings");
            Log.Info("NVIDIA power management mode set to prefer maximum performance.");
        }
        finally
        {
            if (session != IntPtr.Zero)
                TryCall(() => NvApi.DestroySession(session));
            TryCall(() => NvApi.Unload());
        }
    }

    private static void TryCall(Func<int> call)
    {
        try
        {
            call();
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
        }
    }
}

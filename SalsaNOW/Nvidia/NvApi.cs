using System.Runtime.InteropServices;

namespace SalsaNOW.Nvidia;

internal static class NvApi
{
    public const int Ok = 0;

    private const uint InitializeId = 0x0150E828;
    private const uint UnloadId = 0xD22BDD7E;
    private const uint CreateSessionId = 0x0694D52E;
    private const uint DestroySessionId = 0xDAD9CFF8;
    private const uint LoadSettingsId = 0x375DBD6B;
    private const uint SaveSettingsId = 0xFCBC7E14;
    private const uint RestoreAllDefaultsId = 0x5927B094;
    private const uint GetBaseProfileId = 0xDA8466A0;
    private const uint SetSettingId = 0x577DD202;

    private const int SettingSize = 12320;
    private const uint SettingVersion = 0x00013020;
    private const int SettingIdOffset = 4100;
    private const int SettingTypeOffset = 4104;
    private const int CurrentValueOffset = 8220;
    private const uint DwordType = 0;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int NoArguments();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int CreateSessionFunction(out IntPtr session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SessionFunction(IntPtr session);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetBaseProfileFunction(IntPtr session, out IntPtr profile);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SetSettingFunction(IntPtr session, IntPtr profile, IntPtr setting);

    public static NoArguments Initialize => Get<NoArguments>(InitializeId);
    public static NoArguments Unload => Get<NoArguments>(UnloadId);
    public static CreateSessionFunction CreateSession => Get<CreateSessionFunction>(CreateSessionId);
    public static SessionFunction DestroySession => Get<SessionFunction>(DestroySessionId);
    public static SessionFunction LoadSettings => Get<SessionFunction>(LoadSettingsId);
    public static SessionFunction SaveSettings => Get<SessionFunction>(SaveSettingsId);
    public static SessionFunction RestoreAllDefaults => Get<SessionFunction>(RestoreAllDefaultsId);
    public static GetBaseProfileFunction GetBaseProfile => Get<GetBaseProfileFunction>(GetBaseProfileId);
    public static SetSettingFunction SetSetting => Get<SetSettingFunction>(SetSettingId);

    public static IntPtr AllocateDwordSetting(uint settingId, uint value)
    {
        IntPtr setting = Marshal.AllocHGlobal(SettingSize);
        Marshal.Copy(new byte[SettingSize], 0, setting, SettingSize);
        Marshal.WriteInt32(setting, 0, unchecked((int)SettingVersion));
        Marshal.WriteInt32(setting, SettingIdOffset, unchecked((int)settingId));
        Marshal.WriteInt32(setting, SettingTypeOffset, unchecked((int)DwordType));
        Marshal.WriteInt32(setting, CurrentValueOffset, unchecked((int)value));
        return setting;
    }

    public static void Check(int status, string function)
    {
        if (status != Ok)
            throw new InvalidOperationException($"{function} failed with status {status}.");
    }

    private static T Get<T>(uint id) where T : Delegate
    {
        IntPtr function = QueryInterface(id);
        if (function == IntPtr.Zero)
            throw new EntryPointNotFoundException($"NVAPI function 0x{id:X8} could not be resolved. Ensure an NVIDIA GPU driver is installed.");

        return Marshal.GetDelegateForFunctionPointer<T>(function);
    }

    [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr QueryInterface(uint id);
}

namespace Cogs.Windows.NativeInterop.Types;

[StructLayout(LayoutKind.Sequential)]
struct LastInputInfo
{
    [MarshalAs(UnmanagedType.U4)]
    public int Size;
    [MarshalAs(UnmanagedType.U4)]
    public uint Time;
    public static readonly int SizeOf = Marshal.SizeOf(typeof(LastInputInfo));
}

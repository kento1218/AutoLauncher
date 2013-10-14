using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AutoLauncherSrv
{
    public class Win32API
    {
        public static UInt32? GetSessionID(string username)
        {
            IntPtr ppSessionInfo = IntPtr.Zero;
            Int32 sessions = 0;
            Int32 dataSize = Marshal.SizeOf(typeof(Win32API.WTS_SESSION_INFO));
            UInt32? sessionID = null;

            if (WTSEnumerateSessions(IntPtr.Zero, 0, 1, ref ppSessionInfo, ref sessions) != 0)
            {
                Int64 current = (Int64)ppSessionInfo;
                for (int i = 0; i < sessions; i++)
                {
                    WTS_SESSION_INFO si = (WTS_SESSION_INFO) Marshal.PtrToStructure(
                        (IntPtr) current, typeof(WTS_SESSION_INFO));

                    var name = GetUsernameBySessionId(si.SessionID);
                    if (name == username)
                    {
                        sessionID = si.SessionID;
                        break;
                    }

                    current += dataSize;
                }
                WTSFreeMemory(ppSessionInfo);
            }

            return sessionID;
        }

        public static string GetUsernameBySessionId(UInt32 sessionId)
        {
            IntPtr buffer;
            uint strLen;
            var username = "";
            if (WTSQuerySessionInformation(
                IntPtr.Zero, sessionId, WTSInfoClass.WTSUserName, out buffer, out strLen) && strLen > 1)
            {
                username = Marshal.PtrToStringAnsi(buffer); // don't need length as these are null terminated strings
                WTSFreeMemory(buffer);
            }
            return username;
        }

        public static bool CreateProcess(UInt32 sessionID, string cmdline, out Int32? processID)
        {
            IntPtr token = IntPtr.Zero;
            processID = null;

            if (!WTSQueryUserToken(sessionID, out token))
            {
                return false;
            }
            
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);

            PROCESS_INFORMATION pi;

            SECURITY_ATTRIBUTES saProc = new SECURITY_ATTRIBUTES();
            saProc.nLength = Marshal.SizeOf(saProc);
            SECURITY_ATTRIBUTES saThread = new SECURITY_ATTRIBUTES();
            saThread.nLength = Marshal.SizeOf(saThread);

            if (!CreateProcessAsUser(token, null, cmdline, ref saProc, ref saThread,
                false, 0, IntPtr.Zero, null, ref si, out pi))
            {
                return false;
            }

            processID = pi.dwProcessId;
            return true;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQueryUserToken(UInt32 sessionId, out IntPtr Token);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern int WTSEnumerateSessions(
                        System.IntPtr hServer,
                        int Reserved,
                        int Version,
                        ref System.IntPtr ppSessionInfo,
                        ref int pCount);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQuerySessionInformation(
            IntPtr hServer, UInt32 sessionId, WTSInfoClass wtsInfoClass, out IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport("wtsapi32.dll")]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessId;
            public Int32 dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_SESSION_INFO
        {
            public UInt32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        public enum WTSInfoClass
        {
            /// <summary>
            /// A null-terminated string that contains the name of the initial program that Remote Desktop Services runs when the user logs on.
            /// </summary>
            WTSInitialProgram,

            /// <summary>
            /// A null-terminated string that contains the published name of the application that the session is running.
            /// </summary>
            WTSApplicationName,

            /// <summary>
            /// A null-terminated string that contains the default directory used when launching the initial program.
            /// </summary>
            WTSWorkingDirectory,

            /// <summary>
            /// This value is not used.
            /// </summary>
            WTSOEMId,

            /// <summary>
            /// A <B>ULONG</B> value that contains the session identifier.
            /// </summary>
            WTSSessionId,

            /// <summary>
            /// A null-terminated string that contains the name of the user associated with the session.
            /// </summary>
            WTSUserName,

            /// <summary>
            /// A null-terminated string that contains the name of the Remote Desktop Services session. 
            /// </summary>
            /// <remarks>
            /// <B>Note</B>  Despite its name, specifying this type does not return the window station name. 
            /// Rather, it returns the name of the Remote Desktop Services session. 
            /// Each Remote Desktop Services session is associated with an interactive window station. 
            /// Because the only supported window station name for an interactive window station is "WinSta0", 
            /// each session is associated with its own "WinSta0" window station. For more information, see <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/ms687096(v=vs.85).aspx">Window Stations</see>.
            /// </remarks>
            WTSWinStationName,

            /// <summary>
            /// A null-terminated string that contains the name of the domain to which the logged-on user belongs.
            /// </summary>
            WTSDomainName,

            /// <summary>
            /// The session's current connection state. For more information, see <see cref="WTS_CONNECTSTATE_CLASS"/>.
            /// </summary>
            WTSConnectState,

            /// <summary>
            /// A <B>ULONG</B> value that contains the build number of the client.
            /// </summary>
            WTSClientBuildNumber,

            /// <summary>
            /// A null-terminated string that contains the name of the client.
            /// </summary>
            WTSClientName,

            /// <summary>
            /// A null-terminated string that contains the directory in which the client is installed.
            /// </summary>
            WTSClientDirectory,

            /// <summary>
            /// A <B>USHORT</B> client-specific product identifier.
            /// </summary>
            WTSClientProductId,

            /// <summary>
            /// A <B>ULONG</B> value that contains a client-specific hardware identifier. This option is reserved for future use. 
            /// <see cref="WTSQuerySessionInformation"/> will always return a value of 0.
            /// </summary>
            WTSClientHardwareId,

            /// <summary>
            /// The network type and network address of the client. For more information, see <see cref="WTS_CLIENT_ADDRESS"/>.
            /// </summary>
            /// <remarks>The IP address is offset by two bytes from the start of the <B>Address</B> member of the <see cref="WTS_CLIENT_ADDRESS"/> structure.</remarks>
            WTSClientAddress,

            /// <summary>
            /// Information about the display resolution of the client. For more information, see <see cref="WTS_CLIENT_DISPLAY"/>.
            /// </summary>
            WTSClientDisplay,

            /// <summary>
            /// A USHORT value that specifies information about the protocol type for the session. This is one of the following values:<BR/>
            /// 0 - The console session.<BR/>
            /// 1 - This value is retained for legacy purposes.<BR/>
            /// 2 - The RDP protocol.<BR/>
            /// </summary>
            WTSClientProtocolType,

            /// <summary>
            /// This value returns <B>FALSE</B>. If you call <see cref="GetLastError"/> to get extended error information, <B>GetLastError</B> returns <B>ERROR_NOT_SUPPORTED</B>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not used.
            /// </remarks>
            WTSIdleTime,

            /// <summary>
            /// This value returns <B>FALSE</B>. If you call <see cref="GetLastError"/> to get extended error information, <B>GetLastError</B> returns <B>ERROR_NOT_SUPPORTED</B>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not used.
            /// </remarks>
            WTSLogonTime,

            /// <summary>
            /// This value returns <B>FALSE</B>. If you call <see cref="GetLastError"/> to get extended error information, <B>GetLastError</B> returns <B>ERROR_NOT_SUPPORTED</B>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not used.
            /// </remarks>
            WTSIncomingBytes,

            /// <summary>
            /// This value returns <B>FALSE</B>. If you call <see cref="GetLastError"/> to get extended error information, <B>GetLastError</B> returns <B>ERROR_NOT_SUPPORTED</B>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not used.
            /// </remarks>
            WTSOutgoingBytes,

            /// <summary>
            /// This value returns <B>FALSE</B>. If you call <see cref="GetLastError"/> to get extended error information, <B>GetLastError</B> returns <B>ERROR_NOT_SUPPORTED</B>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not used.
            /// </remarks>
            WTSIncomingFrames,

            /// <summary>
            /// This value returns <B>FALSE</B>. If you call <see cref="GetLastError"/> to get extended error information, <B>GetLastError</B> returns <B>ERROR_NOT_SUPPORTED</B>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not used.
            /// </remarks>
            WTSOutgoingFrames,

            /// <summary>
            /// Information about a Remote Desktop Connection (RDC) client. For more information, see <see cref="WTSCLIENT"/>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not supported. 
            /// This value is supported beginning with Windows Server 2008 and Windows Vista with SP1.
            /// </remarks>
            WTSClientInfo,

            /// <summary>
            /// Information about a client session on an RD Session Host server. For more information, see <see cref="WTSINFO"/>.
            /// </summary>
            /// <remarks>
            /// <B>Windows Vista, Windows Server 2003, and Windows XP:</B>  This value is not supported. 
            /// This value is supported beginning with Windows Server 2008 and Windows Vista with SP1.
            /// </remarks>
            WTSSessionInfo
        }
    }
}

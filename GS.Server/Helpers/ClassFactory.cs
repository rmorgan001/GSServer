/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace GS.Server.Helpers
{
    #region C# Definition of IClassFactory
    //
    // Provide a definition of theCOM IClassFactory interface.
    //
    [
        ComImport,												// This interface originated from COM.
        ComVisible(false),										// Must not be exposed to COM!!!
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),		// Indicate that this interface is not IDispatch-based.
        Guid("00000001-0000-0000-C000-000000000046")					// This GUID is the actual GUID of IClassFactory.
    ]
    public interface IClassFactory
    {
        void CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
        void LockServer(bool fLock);
    }
    #endregion

    /// <summary>
    /// Universal ClassFactory.Given a type as a parameter of the
    /// constructor, it implements IClassFactory for any interface
    /// that the class implements. Magic!!!
    /// </summary>
    internal class ClassFactory : IClassFactory
    {
        #region Access to ole32.dll functions for class factories

        // Define two common GUID objects for public usage.
        private static readonly Guid IidIUnknown = new Guid("{00000000-0000-0000-C000-000000000046}");
        private static readonly Guid IidIDispatch = new Guid("{00020400-0000-0000-C000-000000000046}");

        [Flags]
        private enum Clsctx : uint
        {
            //CLSCTX_INPROC_SERVER = 0x1,
            //CLSCTX_INPROC_HANDLER = 0x2,
            ClsctxLocalServer = 0x4,
            //CLSCTX_INPROC_SERVER16 = 0x8,
            //CLSCTX_REMOTE_SERVER = 0x10,
            //CLSCTX_INPROC_HANDLER16 = 0x20,
            //CLSCTX_RESERVED1 = 0x40,
            //CLSCTX_RESERVED2 = 0x80,
            //CLSCTX_RESERVED3 = 0x100,
            //CLSCTX_RESERVED4 = 0x200,
            //CLSCTX_NO_CODE_DOWNLOAD = 0x400,
            //CLSCTX_RESERVED5 = 0x800,
            //CLSCTX_NO_CUSTOM_MARSHAL = 0x1000,
            //CLSCTX_ENABLE_CODE_DOWNLOAD = 0x2000,
            //CLSCTX_NO_FAILURE_LOG = 0x4000,
            //CLSCTX_DISABLE_AAA = 0x8000,
            //CLSCTX_ENABLE_AAA = 0x10000,
            //CLSCTX_FROM_DEFAULT_CONTEXT = 0x20000,
            //CLSCTX_INPROC = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER,
            //CLSCTX_SERVER = CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER,
            //CLSCTX_ALL = CLSCTX_SERVER | CLSCTX_INPROC_HANDLER
        }

        [Flags]
        private enum Regcls : uint
        {
            //REGCLS_SINGLEUSE = 0,
            RegclsMultipleuse = 1,
            //REGCLS_MULTI_SEPARATE = 2,
            RegclsSuspended = 4,
            //REGCLS_SURROGATE = 8
        }
        //
        // CoRegisterClassObject() is used to register a Class Factory
        // into COM's internal table of Class Factories.
        //

        #endregion

        #region Constructor and Private ClassFactory Data

        private readonly Type _mClassType;
        private Guid _mClassId;
        private readonly ArrayList _mInterfaceTypes;

        //private UInt32 m_locked = 0;
        private uint _mCookie;

        public ClassFactory(Type type)
        {
            // // LocalSystem.TraceLogItem("ClassFactory", "Start of initialisation");
            _mClassType = type;
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _mClassId = Marshal.GenerateGuidForType(type);		// Should be nailed down by [Guid(...)]
            ClassContext = (uint)Clsctx.ClsctxLocalServer;	// Default
            Flags = (uint)Regcls.RegclsMultipleuse |			// Default
                        (uint)Regcls.RegclsSuspended;
            _mInterfaceTypes = new ArrayList();

            foreach (var T in type.GetInterfaces())			// Save all of the implemented interfaces
            {
                //     // LocalSystem.TraceLogItem("ClassFactory", "Adding Type: " + T.FullName);
                _mInterfaceTypes.Add(T);
            }
            //  // LocalSystem.TraceLogItem("ClassFactory", "Completed initialisation");
        }

        #endregion

        #region Common ClassFactory Methods

        private uint ClassContext { get; }

        //public Guid ClassId
        //{
        //    get => _mClassId;
        //    set => _mClassId = value;
        //}

        private uint Flags { get; }

        public bool RegisterClassObject()
        {

            // Register the class factory
            var i = NativeMethods.CoRegisterClassObject
                (
                ref _mClassId,
                this,
                ClassContext,
                Flags,
                out _mCookie
                );
            //   // LocalSystem.TraceLogItem("RegisterClassObject", "GUID: " + _mClassId + ", Cookie: " + _mCookie);
            return (i == 0);
        }

        public void RevokeClassObject()
        {
            //   // LocalSystem.TraceLogItem("RevokeClassObject", _mCookie.ToString());
            NativeMethods.CoRevokeClassObject(_mCookie);
        }

        public static void ResumeClassObjects()
        {
            //   // LocalSystem.TraceLogItem("ResumeClassObjects", "Called");
            NativeMethods.CoResumeClassObjects();
        }

        public static void SuspendClassObjects()
        {
            //  // LocalSystem.TraceLogItem("SuspendClassObjects", "Called");
            NativeMethods.CoSuspendClassObjects();
        }
        #endregion

        #region IClassFactory Implementations
        //
        // Implement creation of the type and interface.
        //
        void IClassFactory.CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            //   // LocalSystem.TraceLogItem("CreateInstance", "GUID: " + riid);
            IntPtr nullPtr = new IntPtr(0);
            ppvObject = nullPtr;

            //
            // Handle specific requests for implemented interfaces
            //
            foreach (Type iType in _mInterfaceTypes)
            {
                if (riid == Marshal.GenerateGuidForType(iType))
                {
                    ppvObject = Marshal.GetComInterfaceForObject(Activator.CreateInstance(_mClassType), iType);
                    return;
                }
            }
            //
            // Handle requests for IDispatch or IUnknown on the class
            //

            if (riid == IidIDispatch)
            {
                ppvObject = Marshal.GetIDispatchForObject(Activator.CreateInstance(_mClassType));
            }
            else if (riid == IidIUnknown)
            {
                ppvObject = Marshal.GetIUnknownForObject(Activator.CreateInstance(_mClassType));
            }
            else
            {
                //
                // Oops, some interface that the class doesn't implement
                //
                throw new COMException("No interface", unchecked((int)0x80004002));
            }
        }

        void IClassFactory.LockServer(bool bLock)
        {
            //    // LocalSystem.TraceLogItem("LockServer", "Lock server: " + bLock);
            if (bLock)
                GSServer.CountLock();
            else
                //      LocalServer.UncountLock();
                // Always attempt to see if we need to shutdown this server application.
                //    // LocalSystem.TraceLogItem("LockServer", "Calling ExitIf to check whether we can exit the server");
                GSServer.ExitIf();
        }
        #endregion
    }
}

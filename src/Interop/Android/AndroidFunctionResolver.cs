#if NET5_0_OR_GREATER
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorceryMedia.FFmpeg.Interop.Android
{
    internal class AndroidFunctionResolver : FunctionResolverBase
    {
        protected override nint GetFunctionPointer(nint nativeLibraryHandle, string functionName)
        {
            var func = NativeLibrary.GetExport(nativeLibraryHandle, functionName);

            return func;
        }
        protected override string GetNativeLibraryName(string libraryName, int version) => $"lib{libraryName}";

        protected override nint LoadNativeLibrary(string libraryName)
        {
            var lib = NativeLibrary.Load(libraryName);

            return lib;
        }
    }
}
#endif

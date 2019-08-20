using Llvm.NET;
using Llvm.NET.JIT;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Cordy.Codegen
{
    public sealed class JIT : OrcJit
    {
        public JIT() : base(BuildTargetMachine())
        {
            AddInteropCallback("putchard", new ArgumentedCallbackHandler(PutChard));
            AddInteropCallback("printd", new ArgumentedCallbackHandler(Printd));
        }

        public TextWriter OutputWriter { get; set; } = Console.Out;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void voidCallbackHandler();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate object? CallbackHandler();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void voidArgumentedCallbackHandler(object[] args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate object? ArgumentedCallbackHandler(object[] args);

        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Native callback *MUST NOT* surface managed exceptions")]
        private object Printd(object x)
        {
            try
            {
                OutputWriter.WriteLine(x);
                return null;
            }
            catch
            {
                return null;
            }
        }
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Native callback *MUST NOT* surface managed exceptions")]
        private object PutChard(object x)
        {
            try
            {
                OutputWriter.Write((char)x);
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static TargetMachine BuildTargetMachine()
        {
            var ht = Triple.HostTriple.ToString();
            return Target.FromTriple(ht).CreateTargetMachine(ht, null, null, CodeGenOpt.Default, RelocationMode.Default, CodeModel.JitDefault);
        }
    }
}

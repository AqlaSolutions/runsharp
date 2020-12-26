using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    public abstract class TestBase
    {
        private UnhandledExceptionEventHandler _unhandledExceptionHandler;
        readonly List<object> _exceptions = new List<object>();

        public Action<IList<object>> UnhandledExceptionCheck { get; set; }

        public TestBase()
        {

        }

        [SetUp]
        public void SetExceptionsLanguage()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

#if NET5_0
        static int _inited;

        [SetUp]
        public void InitNet5()
        {
            if (Interlocked.CompareExchange(ref _inited, 1, 0) == 0)
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
#endif

        [SetUp]
        public void UnhandledExceptionRegistering()
        {
            _exceptions.Clear();
            UnhandledExceptionCheck = DefaultExceptionCheck;
            _unhandledExceptionHandler = (s, e) =>
                {
                    _exceptions.Add(e.ExceptionObject);

                    Debug.WriteLine(e.ExceptionObject);
                };

            AppDomain.CurrentDomain.UnhandledException += _unhandledExceptionHandler;
        }

        void DefaultExceptionCheck(IList<object> e)
        {
            Assert.IsTrue(e.Count == 0, string.Join("\r\n\r\n", e.Select(ex => ex.ToString()).ToArray()));
        }
        
        [TearDown]
        public void VerifyUnhandledExceptionOnFinalizers()
        {
            GC.GetTotalMemory(true);

            UnhandledExceptionCheck(_exceptions);
            
            AppDomain.CurrentDomain.UnhandledException -= _unhandledExceptionHandler;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
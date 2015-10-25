using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace TriAxis.RunSharp.Tests
{
    public static class ConsoleTester
    {
        static readonly StringBuilder _capturedContent = new StringBuilder();

        public static string CapturedContent => _capturedContent.ToString();

        public static void AssertAndClear(string data)
        {
            string c = CapturedContent;
            Assert.That(c, Is.EqualTo(data));
            Clear();
        }

        public static void ClearAndStartCapturing()
        {
            Clear();
            IsCapturing = true;
        }

        public static void Clear()
        {
            if (_capturedContent.Length == 0) return;
            _capturedContent.Clear();
            OnContentUpdated?.Invoke(null, new EventArgs());
        }

        public static bool IsCapturing { get; set; }

        public static event EventHandler OnContentUpdated;

        public static void Initialize()
        {
            Console.SetOut(new Listener());
        }

        public class Listener : TextWriter
        {
            public override Encoding Encoding => Encoding.Unicode;

            public override void Write(string value)
            {
                if (!IsCapturing) return;
                _capturedContent.Append(value);
                OnContentUpdated?.Invoke(null, new EventArgs());
            }

            public override void WriteLine()
            {
                if (!IsCapturing) return;
                _capturedContent.AppendLine();
                OnContentUpdated?.Invoke(null, new EventArgs());
            }

            public override void Write(char value)
            {
                if (!IsCapturing) return;
                _capturedContent.Append(value);
                OnContentUpdated?.Invoke(null, new EventArgs());
            }
        }
    }
}
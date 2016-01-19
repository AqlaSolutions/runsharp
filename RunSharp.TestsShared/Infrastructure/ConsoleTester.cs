/*
Copyright(c) 2016, Vladyslav Taranov

MIT License

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace TriAxis.RunSharp
{
    public static class ConsoleTester
    {
        static readonly StringBuilder _capturedContent = new StringBuilder();

        public static string CapturedContent => _capturedContent.ToString();

        public static void AssertAndClear(string data)
        {
            string c = CapturedContent;
            if (data != null)
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
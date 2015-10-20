// Modified by Vladyslav Taranov for AqlaSerializer, 2014
// Originally written in protobuf-net

using System;
using IKVM.Reflection.Emit;
using Type  = IKVM.Reflection.Type;

namespace AqlaSerializer.Compiler
{
    internal sealed class Local : IDisposable
    {
        // public static readonly Local InputValue = new Local(null, null);
        LocalBuilder _value;
        public Type Type => _type;

        public Local AsCopy()
        {
            if (_ctx == null) return this; // can re-use if context-free
            return new Local(_value, _type);
        }
        internal LocalBuilder Value
        {
            get
            {
                if (_value == null)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return _value;
            }
        }
        CompilerContext _ctx;
        public void Dispose()
        {
            if (_ctx != null)
            {
                // only *actually* dispose if this is context-bound; note that non-bound
                // objects are cheekily re-used, and *must* be left intact agter a "using" etc
                _ctx.ReleaseToPool(_value);
                _value = null; 
                _ctx = null;
            }            
            
        }
        private Local(LocalBuilder value, Type type)
        {
            _value = value;
            _type = type;
        }
        private readonly Type _type;
        internal Local(CompilerContext ctx, Type type)
        {
            _ctx = ctx;
            if (ctx != null) { _value = ctx.GetFromPool(type); }
            _type = type;
        }

        public bool IsSame(Local other)
        {
            if((object)this == (object)other) return true;

            object ourVal = _value; // use prop to ensure obj-disposed etc
            return other != null && ourVal == (object)(other._value); 
        }
    }
}
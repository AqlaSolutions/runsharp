/*
protobuf-net is Copyright 2008 Marc Gravell
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
   

AqlaSerializer is Copyright 2015 Vladyslav Taranov

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

// Modified by Vladyslav Taranov for AqlaSerializer, 2015

using System;
using System.Collections.Generic;
using System.Text;
#if FEAT_IKVM
using IKVM.Reflection;
using IKVM.Reflection.Emit;
using Type = IKVM.Reflection.Type;
using MissingMethodException = System.MissingMethodException;
using MissingMemberException = System.MissingMemberException;
using DefaultMemberAttribute = System.Reflection.DefaultMemberAttribute;
using Attribute = IKVM.Reflection.CustomAttributeData;
using BindingFlags = IKVM.Reflection.BindingFlags;
#else
using System.Reflection;
using System.Reflection.Emit;
#endif

using System;

namespace TriAxis.RunSharp
{
    /// <summary>
    /// Represents configuration options for compiling a model to 
    /// a standalone assembly.
    /// </summary>
    public sealed class CompilerOptions
    {
        /// <summary>
        /// Import framework options from an existing type
        /// </summary>
        public void SetFrameworkOptions(Type from, ITypeMapper mapper)
        {
            if (@from == null) throw new ArgumentNullException(nameof(@from));
            AttributeMap[] attribs = AttributeMap.Create(mapper, @from.Assembly);
            foreach (AttributeMap attrib in attribs)
            {
                if (attrib.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                {
                    object tmp;
                    if (attrib.TryGet("FrameworkName", out tmp)) TargetFrameworkName = (string)tmp;
                    if (attrib.TryGet("FrameworkDisplayName", out tmp)) TargetFrameworkDisplayName = (string)tmp;
                    break;
                }
            }
        }

        private string _imageRuntimeVersion;
        private int _metaDataVersion;
        /// <summary>
        /// The TargetFrameworkAttribute FrameworkName value to burn into the generated assembly
        /// </summary>
        public string TargetFrameworkName { get; set; }

        /// <summary>
        /// The TargetFrameworkAttribute FrameworkDisplayName value to burn into the generated assembly
        /// </summary>
        public string TargetFrameworkDisplayName { get; set; }

        public bool SymbolInfo { get; set; }

        /// <summary>
        /// The path for the new dll
        /// </summary>
        public string OutputPath { get; set; }

#if FEAT_IKVM
        /// <summary>
        /// The name of the container that holds the key pair.
        /// </summary>
        public string KeyContainer { get; set; }
        /// <summary>
        /// The path to a file that hold the key pair.
        /// </summary>
        public string KeyFile { get; set; }

        /// <summary>
        /// The public  key to sign the file with.
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// The runtime version for the generated assembly
        /// </summary>
        public string ImageRuntimeVersion { get { return _imageRuntimeVersion; } set { _imageRuntimeVersion = value; } }

        /// <summary>
        /// The runtime version for the generated assembly
        /// </summary>
        public int MetaDataVersion { get { return _metaDataVersion; } set { _metaDataVersion = value; } }

#endif
    }
}
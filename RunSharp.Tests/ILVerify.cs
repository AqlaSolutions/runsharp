// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// adapted https://github.com/dotnet/runtime/blob/10381a2cdc33860f0dc649b24cbb703d23b9ea33/src/coreclr/tools/ILVerify/Program.cs

#if NET5_0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Internal.TypeSystem.Ecma;

namespace ILVerify
{
    class ILVerify : ResolverBase
    {
        readonly Dictionary<string, string> _inputFilePaths = new(StringComparer.OrdinalIgnoreCase); // map of simple name to file path
        readonly Dictionary<string, string> _referenceFilePaths = new(StringComparer.OrdinalIgnoreCase); // map of simple name to file path
        
        public ILVerify(string inputFilePath, string[] reference)
        {
            if (inputFilePath != null) AppendExpandedPaths(_inputFilePaths, inputFilePath, true);

            if (reference != null)
                foreach (var r in reference)
                    AppendExpandedPaths(_referenceFilePaths, r, false);
        }

        public IEnumerable<string> Run()
        {
            var verifier = new Verifier(this, new VerifierOptions { IncludeMetadataTokensInErrorMessages = true });
            verifier.SetSystemModuleName(new AssemblyName("System.Runtime"));
            PEReader peReader = Resolve(_inputFilePaths.Keys.First());
            return verifier.Verify(peReader).Concat(peReader.GetMetadataReader().TypeDefinitions.SelectMany(t => verifier.Verify(peReader, t, true)))
                .Select(r => FormatVerifyMethodsResult(verifier, r));
        }

        static readonly MethodInfo _verifierGetModule = typeof(Verifier).GetMethod("GetModule", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        string FormatVerifyMethodsResult(Verifier verifier, VerificationResult result)
        {
            StringBuilder sb = new StringBuilder();

            void Write(object o)
            {
                sb.Append(o);
            }

            Write("[IL]: Error [");
            if (result.Code != VerifierError.None)
            {
                Write(result.Code);
            }
            else
            {
                Write(result.ExceptionID);
            }
            Write("]: ");

            Write("[");
            var peReader = Resolve(_inputFilePaths.Keys.First());
            EcmaModule module = (EcmaModule) _verifierGetModule.Invoke(verifier,new [] { peReader });
            MetadataReader metadataReader = peReader.GetMetadataReader();
            TypeDefinition typeDef = metadataReader.GetTypeDefinition(metadataReader.GetMethodDefinition(result.Method).GetDeclaringType());
            string typeNamespace = metadataReader.GetString(typeDef.Namespace);
            Write(typeNamespace);
            Write(".");
            string typeName = metadataReader.GetString(typeDef.Name);
            Write(typeName);

            Write("::");
            var method = (EcmaMethod)module.GetMethod(result.Method);
            sb.Append(FormatMethod(method));
            Write("]");

            if (result.Code != VerifierError.None)
            {
                Write("[offset 0x");
                Write(result.GetArgumentValue<int>("Offset").ToString("X8"));
                Write("]");

                if (result.TryGetArgumentValue("Found", out string found))
                {
                    Write("[found ");
                    Write(found);
                    Write("]");
                }

                if (result.TryGetArgumentValue("Expected", out string expected))
                {
                    Write("[expected ");
                    Write(expected);
                    Write("]");
                }

                if (result.TryGetArgumentValue("Token", out int token))
                {
                    Write("[token  0x");
                    Write(token.ToString("X8"));
                    Write("]");
                }
            }

            Write(" ");
            sb.AppendLine(result.Message);

            return sb.ToString();
        }

        private static string FormatMethod(EcmaMethod method)
        {
            StringBuilder sb = new StringBuilder();

            void Write(object o)
            {
                sb.Append(o);
            }
            Write(method.Name);
            Write("(");
            try
            {
                if (method.Signature.Length > 0)
                {
                    bool first = true;
                    for (int i = 0; i < method.Signature.Length; i++)
                    {
                        Internal.TypeSystem.TypeDesc parameter = method.Signature[i];
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            Write(", ");
                        }

                        Write(parameter.ToString());
                    }
                }
            }
            catch
            {
                Write("Error while getting method signature");
            }
            Write(")");
            return sb.ToString();
        }
        protected override PEReader ResolveCore(string simpleName)
        {
            if (_inputFilePaths.TryGetValue(simpleName, out string path) || _referenceFilePaths.TryGetValue(simpleName, out path))
            {
                return new PEReader(File.OpenRead(path));
            }

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == simpleName);
            if (assembly == null) return null;
            path = assembly.Location;
            return new PEReader(File.OpenRead(path));
        }

        /// MIT https://github.com/layomia/dn_runtime/blob/8ef3a757bd205f7ac5f493d235c0a7925a4ae609/src/coreclr/src/tools/Common/CommandLine/CommandLineHelpers.cs
        static void AppendExpandedPaths(Dictionary<string, string> dictionary, string pattern, bool strict)
        {
            bool empty = true;

            string directoryName = Path.GetDirectoryName(pattern);
            string searchPattern = Path.GetFileName(pattern);

            if (directoryName == "")
                directoryName = ".";

            if (Directory.Exists(directoryName))
            {
                foreach (string fileName in Directory.EnumerateFiles(directoryName, searchPattern))
                {
                    string fullFileName = Path.GetFullPath(fileName);

                    string simpleName = Path.GetFileNameWithoutExtension(fileName);

                    if (dictionary.ContainsKey(simpleName))
                    {
                        if (strict)
                        {
                            throw new Exception("Multiple input files matching same simple name " +
                                fullFileName + " " + dictionary[simpleName]);
                        }
                    }
                    else
                    {
                        dictionary.Add(simpleName, fullFileName);
                    }

                    empty = false;
                }
            }

            if (empty)
            {
                if (strict)
                {
                    throw new Exception("No files matching " + pattern);
                }
                else
                {
                    Console.WriteLine("Warning: No files matching " + pattern);
                }
            }
        }
    }

}
#endif
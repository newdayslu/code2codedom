﻿using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System;
using System.Linq;
using LinqToCodedom.Extensions;

namespace LinqToCodedom
{
    public class CodeDomGenerator
    {
        private List<CodeNamespace> _namespaces = new List<CodeNamespace>();

        private System.Collections.Specialized.StringCollection _assemblies =
            new System.Collections.Specialized.StringCollection() { "System.dll" };

        public enum Language { CSharp, VB };

        public CodeDomGenerator()
        {
        }

        public static CodeDomProvider CreateProvider(Language provider)
        {
            var providerOptions = new Dictionary<string, string>(); providerOptions.Add("CompilerVersion", "v3.5");

            switch (provider)
            {
                case Language.VB:
                    return new Microsoft.VisualBasic.VBCodeProvider(providerOptions);

                case Language.CSharp:
                default:
                    return new Microsoft.CSharp.CSharpCodeProvider(providerOptions);
            }
        }

        public CodeNamespace AddNamespace(string namespaceName)
        {
            CodeNamespace codeNamespace = new CodeNamespace(namespaceName);
            _namespaces.Add(codeNamespace);

            return codeNamespace;
        }

        public CodeNamespace AddNamespace(CodeNamespace codeNamespace)
        {
            _namespaces.Add(codeNamespace);

            return codeNamespace;
        }

        public CodeDomGenerator AddReference(string referencedAssembly)
        {
            _assemblies.Add(referencedAssembly);

            return this;
        }

        class Pair<T, T2>
        {
            public T First;
            public T2 Second;
            public Pair(T first, T2 second)
            {
                First = first;
                Second = second;
            }
        }

        public CodeCompileUnit GetCompileUnit(Language language)
        {
            // Create a new CodeCompileUnit to contain 
            // the program graph.
            CodeCompileUnit compileUnit = new CodeCompileUnit();

            foreach (CodeNamespace ns in _namespaces)
            {
                CodeNamespace ns2add = ns;
                for (int j = 0; j < ns.Types.Count; j++)
                {
                    CodeTypeDeclaration c = ns.Types[j];
                    List<Pair<int, CodeTypeMember>> toReplace = new List<Pair<int, CodeTypeMember>>();
                    for (int i = 0; i < c.Members.Count; i++)
                    {
                        CodeTypeMember m = c.Members[i];
                        CodeTypeMember newMember = ProcessMember(m, language);
                        if (newMember != m)
                            toReplace.Add(new Pair<int, CodeTypeMember>(i, newMember));
                    }
                    if (toReplace.Count > 0)
                    {
                        if (ns2add == ns)
                            ns2add = ns.Clone() as CodeNamespace;
                        
                        c = ns2add.Types[j];
                        foreach (Pair<int, CodeTypeMember> p in toReplace)
                        {
                            int idx = p.First;
                            c.Members.RemoveAt(idx);
                            c.Members.Insert(idx, p.Second);
                        }
                    }
                }
                compileUnit.Namespaces.Add(ns2add);
            }

            return compileUnit;
        }

        private CodeTypeMember ProcessMember(CodeTypeMember m, Language language)
        {
            if (typeof(CodeMemberMethod).IsAssignableFrom(m.GetType()))
                return PropcessMethod(m as CodeMemberMethod, language);
            return m;
        }

        private CodeMemberMethod PropcessMethod(CodeMemberMethod method, Language language)
        {
            if (language == Language.VB)
            {
                if (method.PrivateImplementationType != null)
                {
                    CodeMemberMethod newMethod = method.Clone() as CodeMemberMethod;
                    newMethod.ImplementationTypes.Add(method.PrivateImplementationType);
                    newMethod.PrivateImplementationType = null;
                    return newMethod;
                }
            }
            return method;
        }

        public Assembly Compile()
        {
            return Compile(null);
        }

        public Assembly Compile(string assemblyPath)
        {
            return Compile(assemblyPath, Language.CSharp);
        }

        public Assembly Compile(string assemblyPath, Language language)
        {
            CompilerParameters options = new CompilerParameters();
            options.IncludeDebugInformation = false;
            options.GenerateExecutable = false;
            options.GenerateInMemory = (assemblyPath == null);

            foreach (string refAsm in _assemblies)
                options.ReferencedAssemblies.Add(refAsm);

            if (assemblyPath != null)
                options.OutputAssembly = assemblyPath.Replace('\\', '/');

            using (CodeDomProvider codeProvider = CreateProvider(language))
            {
                CompilerResults results =
                   codeProvider.CompileAssemblyFromDom(options, GetCompileUnit(language));

                if (results.Errors.Count == 0)
                    return results.CompiledAssembly;

                // Process compilation errors
                Console.WriteLine("Compilation Errors:");

                foreach (string outpt in results.Output)
                    Console.WriteLine(outpt);

                foreach (CompilerError err in results.Errors)
                    Console.WriteLine(err.ToString());
            }

            return null;
        }

        public string GenerateCode(Language language)
        {
            StringBuilder sb = new StringBuilder();

            using (TextWriter tw = new IndentedTextWriter(new StringWriter(sb)))
            {
                using (CodeDomProvider codeProvider = CreateProvider(language))
                {
                    codeProvider.GenerateCodeFromCompileUnit(GetCompileUnit(language), tw, new CodeGeneratorOptions());
                }
            }

            return sb.ToString();
        }
    }
}

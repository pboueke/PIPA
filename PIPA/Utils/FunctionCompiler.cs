using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace PIPA.Utils
{
    /// <summary>
    /// https://stackoverflow.com/questions/1707854/parse-string-to-c-sharp-lambda-func
    /// </summary>
    public static class FunctionCompiler
    {
        const string template =                                                           "" +
            "using System;                                                                 " +
            "using Newtonsoft.Json;                                                        " +
            "using System.Collections.Generic;                                             " +
            "using System.Linq;                                                            " +
            "class DelegateContainer {{                                                    " +
            "    public Func<dynamic, {0}> Function {{ get; set; }}                        " +
            "                                                                              " +
            "    public DelegateContainer() {{                                             " +
            "        Function = delegate(dynamic x) {{ {1} }};                             " +
            "    }}                                                                        " +
            "}}                                                                            " ;

        public static Func<dynamic, T> CompileLambda<T>(string lambda)
        {
            string source = string.Format(template, typeof(T).FullName, lambda);
            Assembly a;
            using (CSharpCodeProvider provider = new CSharpCodeProvider()) {
                List<string> assemblies = new List<string>();
                foreach (Assembly x in AppDomain.CurrentDomain.GetAssemblies()) {
                    try {
                        assemblies.Add(x.Location);
                    }
                    catch (NotSupportedException) {/* Console.WriteLine("Error Compiling User Code: Framework NotSupportedException"); */}
                }

                CompilerResults r = provider.CompileAssemblyFromSource(new CompilerParameters(assemblies.ToArray()) { GenerateExecutable = false, GenerateInMemory = true }, source);
                if (r.Errors.HasErrors)
                    throw new Exception("Errors compiling delegate: " + string.Join(Environment.NewLine, r.Errors.OfType<CompilerError>().Select(e => e.ErrorText).ToArray()));
                a = r.CompiledAssembly;
            }
            
            object o = a.CreateInstance("DelegateContainer");
            return (Func<dynamic, T>)(o.GetType().GetProperty("Function").GetValue(o));
        }         
    }
}

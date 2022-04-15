using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = AnalyzerSecond.Test.CSharpCodeFixVerifier<
    AnalyzerSecond.AnalyzerSecondAnalyzer,
    AnalyzerSecond.AnalyzerSecondCodeFixProvider>;

namespace AnalyzerSecond.Test
{
    [TestClass]
    public class AnalyzerSecondUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestEmpty()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNotChanges1()
        {
            var test = @"class Program
{
    public void funct(out int c)
    {
        c = 5;
    }
    public static void Main()
    {
    }
}";
    await VerifyCS.VerifyAnalyzerAsync(test);
        }
        [TestMethod]
        public async Task TestNotChanges2()
        {
            var test = @"class Program
{
    public void funct(out int c, int d)
    {
        c = 5;
    }
    public static void Main()
    {
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNotChanges3()
        {
            var test = @"class Program
{
    public void funct(int a, out int c)
    {
        c = 5;
    }
    public static void Main()
    {
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNotChanges4()
        {
            var test = @"class Program
{
    public void funct(int b, out int c, int a)
    {
        c = 5;
    }
    public static void Main()
    {
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
class Program
{
    [|public void funct(out int a, out int b, out int c)
    {
        a = 5;
        b = 6;
        c = 5;
    }|]
    public static void Main()
    {
    }
}
", @"
class Program
{
    public class ClassForfunct
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }

        public ClassForfunct(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    public void funct(ClassForfunct outparam)
    {
int a;
        int b;
        int c;
        a = 5;
        b = 6;
        c = 5;
outparam = new ClassForfunct(a, b, c);
    }
    public static void Main()
    {
    }
}
");
        }
        [TestMethod]
        public async Task TestMethod3()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
class Program
{
    [|public void funct(out int a, out int b, out int c, int d)
    {
        a = 5;
        b = 6;
        c = 5;
    }|]
    public static void Main()
    {
    }
}
", @"
class Program
{
    public class ClassForfunct
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }

        public ClassForfunct(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    public void funct(int d, ClassForfunct outparam)
    {
int a;
        int b;
        int c;
        a = 5;
        b = 6;
        c = 5;
outparam = new ClassForfunct(a, b, c);
    }
    public static void Main()
    {
    }
}
");
        }
    }
}

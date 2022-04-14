using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = AnalyzerTemplate.Test.CSharpCodeFixVerifier<
    AnalyzerTemplate.AnalyzerTemplateAnalyzer,
    AnalyzerTemplate.AnalyzerTemplateCodeFixProvider>;

namespace AnalyzerTemplate.Test
{
    [TestClass]
    public class AnalyzerTemplateUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestEmpty()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestNormNaming()
        {
            var test = @"using System;

class Program
{
    static void Main()
    {
        bool a = true;
    }
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestDeter()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva = false;|]
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava = !false;
    }
}
");
        }
        [TestMethod]
        public async Task TestDefen()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva = false;|]
        notAva = false;
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava = !false;
        ava = !false;
    }
}
");
        }
        [TestMethod]
        public async Task TestDeterInOtherArg()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva = false;|]
        bool r = notAva;
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava = !false;
        bool r = !ava;
    }
}
");
        }
        [TestMethod]
        public async Task TestUnianLogicalExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva = false;|]
        if(notAva){Console.WriteLine();}
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava = !false;
        if(!ava){Console.WriteLine();}
    }
}
");
        }
        [TestMethod]
        public async Task TestLeftLogicalExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva = false;|]
        if(notAva&&true){Console.WriteLine();}
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava = !false;
        if(!ava &&true){Console.WriteLine();}
    }
}
");
        }
        [TestMethod]
        public async Task TestRightLogicalExpression()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva = false;|]
        if(true&&notAva){Console.WriteLine();}
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava = !false;
        if(true&& !ava){Console.WriteLine();}
    }
}
");
        }
        [TestMethod]
        public async Task TestDeterWithoutDefen()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva;|]
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava;
    }
}
");
        }
        [TestMethod]
        public async Task TestManyDeterWithoutDefen()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva, anonc;|]
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava, anonc;
    }
}
");
        }
        [TestMethod]
        public async Task TestArgInMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System;

class Program
{
    static void Main()
    {
        [|bool notAva = false;|]
        Console.WriteLine(notAva);
    }
}
", @"
using System;

class Program
{
    static void Main()
    {
        bool ava = !false;
        Console.WriteLine(!ava);
    }
}
");
        }
    }
}

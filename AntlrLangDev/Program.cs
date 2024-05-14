using System.Globalization;
using Antlr4.Runtime;

namespace AntlrLangDev;

class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        if (args.Length == 2 && File.Exists(args[1]))
        {
            RunScript(args[1]);
        }
        else
        {
            RunScript("testscript.txt");
        }
    }

    private static void RunScript(string path)
    {
        string content = File.ReadAllText(path);
        var inputStream = new AntlrInputStream(content);
        var lexer = new GScriptLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GScriptParser(tokenStream);
        parser.AddErrorListener(new GScriptErrorListener());
        var context = parser.program();
        var visitor = new GScriptVisitor();
        visitor.Visit(context);
    }
}
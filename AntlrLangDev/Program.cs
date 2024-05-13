using System.Globalization;
using Antlr4.Runtime;

namespace AntlrLangDev;

class Program
{
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        RunScript("testscript.txt");
    }

    private static void RunScript(string path)
    {
        string content = File.ReadAllText(path);

        var inputStream = new AntlrInputStream(content);

        var lexer = new GScriptLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GScriptParser(tokenStream);
        var context = parser.program();
        var visitor = new GScriptVisitor();
        visitor.Visit(context);

        void RunScript(string path){

        }

        RunScript("");
    }
}
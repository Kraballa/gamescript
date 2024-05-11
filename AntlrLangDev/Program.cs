using Antlr4.Runtime;

namespace AntlrLangDev;

class Program
{
    public static void Main(string[] args)
    {
        string filename = "demoscript.scr";
        string content = File.ReadAllText(filename);

        var inputStream = new AntlrInputStream(content);

        var lexer = new GScriptLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new GScriptParser(tokenStream);
        var context = parser.program();
        var visitor = new GScriptVisitor();
        visitor.Visit(context);

    }
}
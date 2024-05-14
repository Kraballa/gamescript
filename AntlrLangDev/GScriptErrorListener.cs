using Antlr4.Runtime;

namespace AntlrLangDev
{
    public class GScriptErrorListener : IAntlrErrorListener<IToken>
    {
        public void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            //we immediately throw since continuing execution would immediately end in undefined behavior
            throw new Exception($"parser encountered unexpected input at line {14}.");
        }
    }
}
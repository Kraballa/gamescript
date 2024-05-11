
namespace AntlrLangDev
{
    internal class GScriptVisitor : GScriptBaseVisitor<object?>
    {
        private Dictionary<string, object?> Variables { get; } = new();


        public override object VisitAssignment([Antlr4.Runtime.Misc.NotNull] GScriptParser.AssignmentContext context)
        {
            string name = context.IDENTIFIER().GetText();
            object? value = Visit(context.expression());

            Variables[name] = value;
            
            return null;
        }

        public override object VisitConstant([Antlr4.Runtime.Misc.NotNull] GScriptParser.ConstantContext context)
        {
            if(context.INTEGER() is {} i){
                return int.Parse(i.GetText());
            }
            if(context.FLOAT() is {} f){
                return float.Parse(f.GetText());
            }
            if(context.STRING() is {} s){
                return s.GetText()[1..^1];
            }
            if(context.BOOL() is {} b){
                return bool.Parse(b.GetText());
            }
            if(context.NULL() is {}){
                return null;
            }
            throw new NotImplementedException();
        }
    }
}


using Antlr4.Runtime.Misc;

namespace AntlrLangDev
{
    internal class GScriptVisitor : GScriptBaseVisitor<object?>
    {
        private Dictionary<string, object> Variables { get; } = new();


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

        public override object VisitIdentifierExpression([NotNull] GScriptParser.IdentifierExpressionContext context)
        {
            var varname = context.IDENTIFIER().GetText();
            if(!Variables.ContainsKey(varname)){
                throw new Exception($"error, variable {varname} not found");
            }
            return Variables[varname];
        }

        public override object VisitNegatedExpression([NotNull] GScriptParser.NegatedExpressionContext context)
        {
            var varname = context.expression().GetText();
            if(!Variables.ContainsKey(varname)){
                throw new Exception($"error, variable {varname} not found");
            }
            var variable = Variables[varname];
            if(variable.GetType() == typeof(bool)){
                return !(bool)variable;
            }
            throw new Exception($"error, variable of type {variable.GetType()} cannot be negated.");
        }

        public override object VisitMultExpression([NotNull] GScriptParser.MultExpressionContext context)
        {
            var expressions = context.expression();
            bool isInt = true;

            foreach(var expression in expressions){
                
            }

            return base.VisitMultExpression(context);
        }
    }
}

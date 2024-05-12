
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
            Console.WriteLine($"{name} -> {value}");
            return null;
        }

        public override object VisitConstant([Antlr4.Runtime.Misc.NotNull] GScriptParser.ConstantContext context)
        {
            if (context.INTEGER() is { } i)
            {
                return int.Parse(i.GetText());
            }
            if (context.FLOAT() is { } f)
            {
                return float.Parse(f.GetText());
            }
            if (context.STRING() is { } s)
            {
                return s.GetText()[1..^1];
            }
            if (context.BOOL() is { } b)
            {
                return bool.Parse(b.GetText());
            }
            if (context.NULL() is { })
            {
                return null;
            }
            throw new NotImplementedException();
        }

        public override object VisitIdentifierExpression([NotNull] GScriptParser.IdentifierExpressionContext context)
        {
            var varname = context.IDENTIFIER().GetText();
            if (!Variables.ContainsKey(varname))
            {
                throw new Exception($"error, variable {varname} not found");
            }
            return Variables[varname];
        }

        public override object VisitNegatedExpression([NotNull] GScriptParser.NegatedExpressionContext context)
        {
            var value = Visit(context.expression());

            if (value.GetType() == typeof(bool))
            {
                return !(bool)value;
            }
            throw new Exception($"error, variable of type {value.GetType()} cannot be negated.");
        }

        public override object VisitMultExpression([NotNull] GScriptParser.MultExpressionContext context)
        {
            var expressions = context.expression();
            var op = context.multOp();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            var type1 = res1.GetType();
            var type2 = res2.GetType();

            if ((type1 != typeof(int) && type1 != typeof(float)) ||
            (type2 != typeof(int) && type2 != typeof(float)))
            {
                throw new Exception("error, invalid operator for mult operation");
            }

            bool isInt = type1 == typeof(int) && type2 == typeof(int);

            switch (op.GetText())
            {
                case "*":
                    if (isInt)
                    {
                        return (int)res1 * (int)res2;
                    }
                    else
                    {
                        return (float)res1 * (float)res2;
                    }
                case "/":
                    if (isInt)
                    {
                        return (int)res1 / (int)res2;
                    }
                    else
                    {
                        return (float)res1 / (float)res2;
                    }
                case "%":
                    if (isInt)
                    {
                        return (int)res1 % (int)res2;
                    }
                    else
                    {
                        return (float)res1 % (float)res2;
                    }
            }
            throw new Exception("error, invalid mult operation");
        }

        public override object VisitAddExpression([NotNull] GScriptParser.AddExpressionContext context)
        {
            var expressions = context.expression();
            var op = context.addOp();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            var type1 = res1.GetType();
            var type2 = res2.GetType();

            if (res1 == null || res2 == null)
            {
                throw new Exception("error, invalid operators for add operation");
            }

            //string concatenation
            if(op.GetText() == "+" && (type1 == typeof(string) || type2 == typeof(string))){
                return res1.ToString() + res2.ToString();
            }

            bool isInt = type1 == typeof(int) && type2 == typeof(int);

            switch (op.GetText())
            {
                case "+":
                    if (isInt)
                    {
                        return (int)res1 + (int)res2;
                    }
                    else
                    {
                        return (float)res1 + (float)res2;
                    }
                case "-":
                    if (isInt)
                    {
                        return (int)res1 - (int)res2;
                    }
                    else
                    {
                        return (float)res1 - (float)res2;
                    }
            }
            throw new Exception("error, invalid add operation");
        }

        public override object VisitBoolExpression([NotNull] GScriptParser.BoolExpressionContext context)
        {
            var expressions = context.expression();
            var op = context.boolOp();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            if (res1.GetType() != typeof(bool) || res2.GetType() != typeof(bool))
            {
                throw new Exception("error, non-boolean value used for bool operator");
            }

            switch (op.GetText())
            {
                case "&":
                    return (bool)res1 & (bool)res2;
                case "|":
                    return (bool)res1 | (bool)res2;
            }

            throw new Exception("error, invalid bool operation");
        }

        public override object VisitEnclosedExpression([NotNull] GScriptParser.EnclosedExpressionContext context)
        {
            return Visit(context.expression());
        }
    }
}

﻿
using System.Linq.Expressions;
using Antlr4.Runtime.Misc;

namespace AntlrLangDev
{
    internal class GScriptVisitor : GScriptBaseVisitor<object?>
    {
        private readonly Dictionary<string, object> Variables = new();
        private readonly Dictionary<string, Func<object[], object>> Functions = new();

        public GScriptVisitor()
        {
            Functions.Add("print", PrintOp);
        }

        private object PrintOp(object[] args)
        {
            Console.WriteLine(args[0].ToString());
            return null;
        }


        public override object VisitAssignment([NotNull] GScriptParser.AssignmentContext context)
        {
            string name = context.IDENTIFIER().GetText();
            object? value = Visit(context.expression());
            Variables[name] = value;
            //Console.WriteLine($"{name} -> {value}");
            return null;
        }

        public override object VisitConstant([NotNull] GScriptParser.ConstantContext context)
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
            var op = context.addOp().GetText();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            if (res1 == null || res2 == null)
            {
                throw new Exception("error, invalid operators for add operation");
            }

            var type1 = res1.GetType();
            var type2 = res2.GetType();

            //string concatenation
            if (op == "+" && (type1 == typeof(string) || type2 == typeof(string)))
            {
                return res1.ToString() + res2.ToString();
            }

            bool isInt = type1 == typeof(int) && type2 == typeof(int);

            switch (op)
            {
                case "+":
                    if (isInt)
                    {
                        return (int)res1 + (int)res2;
                    }
                    else
                    {
                        return (float)((double)res1 + (double)res2);
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

        public override object VisitCompareExpression([NotNull] GScriptParser.CompareExpressionContext context)
        {
            var expressions = context.expression();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            var type1 = res1.GetType();
            var type2 = res2.GetType();

            if (type1 == null || type2 == null)
            {
                throw new Exception("error, compare operation not defined for null type");
            }

            var op = context.compareOp().GetText();

            if (type1 == typeof(string) || type2 == typeof(string))
            {
                if (type1 != typeof(string) || type2 != typeof(string))
                {
                    throw new Exception("error, can't compare string to non-string");
                }
                switch (op)
                {
                    case "==":
                        return (string)res1 == (string)res2;
                    case "!=":
                        return (string)res1 != (string)res2;
                    default:
                        throw new Exception($"error, invalid operation for string comparison: {op}");
                }
            }

            if (type1 == typeof(bool) || type2 == typeof(bool))
            {
                switch (op)
                {
                    case "==":
                        return IsTruthy(res1) == IsTruthy(res2);
                    case "!=":
                        return IsTruthy(res1) != IsTruthy(res2);
                    default:
                        throw new Exception($"error, invalid operation for bool-ish comparison: {op}");
                }
            }

            bool isInt = type1 == typeof(int) && type2 == typeof(int);

            switch (op)
            {
                case "==":
                    if (isInt)
                    {
                        return (int)res1 == (int)res2;
                    }
                    else
                    {
                        return (float)res1 != (float)res2;
                    }
                case "!=":
                    if (isInt)
                    {
                        return (int)res1 != (int)res2;
                    }
                    else
                    {
                        return (float)res1 != (float)res2;
                    }
                case ">":
                    if (isInt)
                    {
                        return (int)res1 > (int)res2;
                    }
                    else
                    {
                        return (float)res1 > (float)res2;
                    }
                case "<":
                    if (isInt)
                    {
                        return (int)res1 < (int)res2;
                    }
                    else
                    {
                        return (float)res1 < (float)res2;
                    }
                case ">=":
                    if (isInt)
                    {
                        return (int)res1 >= (int)res2;
                    }
                    else
                    {
                        return (float)res1 >= (float)res2;
                    }
                case "<=":
                    if (isInt)
                    {
                        return (int)res1 <= (int)res2;
                    }
                    else
                    {
                        return (float)res1 <= (float)res2;
                    }
            }
            throw new Exception($"error, unknown operation {op}");
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

        public override object VisitWhileBlock([NotNull] GScriptParser.WhileBlockContext context)
        {
            while (IsTruthy(Visit(context.expression())))
            {
                Visit(context.block());
            }
            return null;
        }

        public override object VisitIfBlock([NotNull] GScriptParser.IfBlockContext context)
        {
            if (IsTruthy(Visit(context.expression())))
            {
                Visit(context.block());
            }
            else
            {
                var elifBlock = context.elseIfBlock();
                if (elifBlock != null)
                {
                    Visit(elifBlock);
                }
            }

            return null;
        }

        public override object VisitFunctionCall([NotNull] GScriptParser.FunctionCallContext context)
        {
            var ident = context.IDENTIFIER().GetText();
            if (!Functions.ContainsKey(ident))
            {
                throw new Exception($"error, function {ident} not found.");
            }

            int numExpr = (context.children.Count - 2) / 2;
            object[] _params = new object[numExpr];
            for (int i = 0; i < _params.Length; i++)
            {
                object? ret = Visit(context.children[i * 2 + 2]);
                if (ret == null)
                    throw new Exception($"error, parameter at index {i} evaluated to null");
                _params[i] = (object)ret;
            }

            Functions[ident].Invoke(_params);
            return null;
        }

        private bool IsTruthy(object? value)
        {
            if (value is bool b)
            {
                return b;
            }
            if (value is int i)
            {
                return i > 0;
            }
            if (value is float f)
            {
                return f > 0f;
            }

            throw new Exception($"error, can't decide truthiness of value {value}");
        }
    }
}

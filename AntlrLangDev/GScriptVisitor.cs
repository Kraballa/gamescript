
using Antlr4.Runtime.Misc;

namespace AntlrLangDev
{

    internal class GScriptVisitor : GScriptBaseVisitor<object?>
    {

        public readonly StackDict<object> Memory = new();

        private readonly Dictionary<string, Func<object[], object>> ExternalFuncts = new();
        private readonly StackDict<NativeFuncData> NativeFuncts = new();
        private readonly StackDict<object> ParamMemory = new();

        private Random rand = new Random();

        public GScriptVisitor()
        {
            ExternalFuncts.Add("print", PrintOp);
            ExternalFuncts.Add("rand", RandomOp);
        }

        private object PrintOp(object[] args)
        {
            Console.WriteLine(args[0].ToString());
            return null;
        }

        private object RandomOp(object[] args)
        {
            return (float)rand.NextDouble();
        }

        public override object? VisitAssignment([NotNull] GScriptParser.AssignmentContext context)
        {
            string name = context.IDENTIFIER().GetText();
            object? value = Visit(context.expression());
            if (value == null)
                throw new Exception($"(line {context.Start.Line}) error, variable {name} is null");

            var scope = context.scope();

            if (scope != null && scope.GetText() == "global")
            {
                Memory[name] = value;
                return null;
            }

            if (ParamMemory.ContainsKey(name))
            {
                ParamMemory[name] = value;
            }
            else
            {
                Memory[name] = value;
            }
            return null;
        }

        public override object? VisitConstant([NotNull] GScriptParser.ConstantContext context)
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
            var scope = context.scope();
            if (scope != null && scope.GetText() == "global")
            {
                if (!Memory.ContainsKey(varname))
                {
                    throw new Exception($"(line {context.Start.Line}) error, cannot create new variable {varname} in global scope.");
                }
                return Memory[varname];
            }
            else
            {
                if (ParamMemory.ContainsKey(varname))
                {
                    return ParamMemory[varname];
                }
                else if (Memory.ContainsKey(varname))
                {
                    return Memory[varname];
                }
            }
            throw new Exception($"(line {context.Start.Line}) error, variable {varname} not found");
        }

        public override object VisitTypecastExpression([NotNull] GScriptParser.TypecastExpressionContext context)
        {
            string targetType = context.type().GetText();
            object? var = Visit(context.expression());
            if (var == null)
            {
                throw new Exception($"(line {context.Start.Line}) error, trying to cast null expression");
            }
            Type varType = var.GetType();

            if (targetType == "string")
            {
                return var.ToString();
            }
            if (targetType == "bool")
            {
                return IsTruthy(var);
            }
            if (targetType == "float")
            {
                if (varType == typeof(float)) return var;
                if (varType == typeof(int)) return (int)(float)var;
            }
            else if (targetType == "int")
            {
                if (varType == typeof(float)) return (int)(float)var;
                if (varType == typeof(int)) return var;
            }
            throw new Exception($"(line {context.Start.Line}) error, conversion from {varType} to {targetType} not supported");
        }

        public override object VisitNegatedExpression([NotNull] GScriptParser.NegatedExpressionContext context)
        {
            var value = Visit(context.expression());
            bool? logicValue = IsTruthy(value);
            if (logicValue != null)
            {
                return !logicValue;
            }
            throw new Exception($"(line {context.Start.Line}) error, variable of type {value.GetType()} cannot be bool-negated.");
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
                throw new Exception($"error, invalid operator '{op}' for mult operation");
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
                        return Convert.ToSingle(res1) * Convert.ToSingle(res2);
                    }
                case "/":
                    if (isInt)
                    {
                        return (int)res1 / (int)res2;
                    }
                    else
                    {
                        return Convert.ToSingle(res1) / Convert.ToSingle(res2);
                    }
                case "%":
                    if (isInt)
                    {
                        return (int)res1 % (int)res2;
                    }
                    else
                    {
                        return Convert.ToSingle(res1) % Convert.ToSingle(res2);
                    }
            }
            throw new Exception($"error, invalid mult operation '{op}'");
        }

        public override object VisitAddExpression([NotNull] GScriptParser.AddExpressionContext context)
        {
            var expressions = context.expression();
            var op = context.addOp().GetText();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            if (res1 == null || res2 == null)
            {
                throw new Exception($"error, invalid add operator '{op}'.");
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
                        return Convert.ToSingle(res1) - Convert.ToSingle(res2);
                    }
            }
            throw new Exception($"error, invalid add operation '{op}'");
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
                throw new Exception($"(line {context.Start.Line}) error, compare operation not defined for null type");
            }

            var op = context.compareOp().GetText();

            if (type1 == typeof(string) || type2 == typeof(string))
            {
                if (type1 != typeof(string) || type2 != typeof(string))
                {
                    throw new Exception($"(line {context.Start.Line}) error, can't compare string to non-string");
                }
                switch (op)
                {
                    case "==":
                        return (string)res1 == (string)res2;
                    case "!=":
                        return (string)res1 != (string)res2;
                    default:
                        throw new Exception($"(line {context.Start.Line}) error, invalid operation for string comparison: {op}");
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
                        throw new Exception($"(line {context.Start.Line}) error, invalid operation for bool-ish comparison: {op}");
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
                        return Convert.ToSingle(res1) != Convert.ToSingle(res2);
                    }
                case "!=":
                    if (isInt)
                    {
                        return (int)res1 != (int)res2;
                    }
                    else
                    {
                        return Convert.ToSingle(res1) != Convert.ToSingle(res2);
                    }
                case ">":
                    if (isInt)
                    {
                        return (int)res1 > (int)res2;
                    }
                    else
                    {
                        return Convert.ToSingle(res1) > Convert.ToSingle(res2);
                    }
                case "<":
                    if (isInt)
                    {
                        return (int)res1 < (int)res2;
                    }
                    else
                    {
                        return Convert.ToSingle(res1) < Convert.ToSingle(res2);
                    }
                case ">=":
                    if (isInt)
                    {
                        return (int)res1 >= (int)res2;
                    }
                    else
                    {
                        return Convert.ToSingle(res1) >= Convert.ToSingle(res2);
                    }
                case "<=":
                    if (isInt)
                    {
                        return (int)res1 <= (int)res2;
                    }
                    else
                    {
                        return Convert.ToSingle(res1) <= Convert.ToSingle(res2);
                    }
            }
            throw new Exception($"(line {context.Start.Line}) error, unknown operation {op}");
        }

        public override object VisitAndExpression([NotNull] GScriptParser.AndExpressionContext context)
        {
            var expressions = context.expression();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            if (res1.GetType() != typeof(bool) || res2.GetType() != typeof(bool))
            {
                throw new Exception($"(line {context.Start.Line}) error, non-boolean value used for bool operator");
            }
            return (bool)res1 & (bool)res2;
        }

        public override object VisitOrExpression([NotNull] GScriptParser.OrExpressionContext context)
        {
            var expressions = context.expression();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            if (res1.GetType() != typeof(bool) || res2.GetType() != typeof(bool))
            {
                throw new Exception($"(line {context.Start.Line}) error, non-boolean value used for bool operator");
            }
            return (bool)res1 | (bool)res2;
        }

        public override object VisitEnclosedExpression([NotNull] GScriptParser.EnclosedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override object VisitWhileBlock([NotNull] GScriptParser.WhileBlockContext context)
        {
            Memory.EnterBlock();
            NativeFuncts.EnterBlock();
            while (IsTruthy(Visit(context.expression())))
            {
                Visit(context.block());
            }
            Memory.ExitBlock();
            NativeFuncts.ExitBlock();
            return null;
        }

        public override object VisitIfBlock([NotNull] GScriptParser.IfBlockContext context)
        {
            Memory.EnterBlock();
            NativeFuncts.EnterBlock();
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
            Memory.ExitBlock();
            NativeFuncts.ExitBlock();
            return null;
        }

        public override object VisitFunctionDefinition([NotNull] GScriptParser.FunctionDefinitionContext context)
        {
            var idtfs = context.IDENTIFIER();
            string funcName = idtfs[0].GetText();

            if (ExternalFuncts.ContainsKey(funcName))
            {
                throw new Exception($"(line {context.Start.Line}) error, can't reuse external function name '{funcName}'");
            }
            if (NativeFuncts.Peek().ContainsKey(funcName))
            {
                throw new Exception($"(line {context.Start.Line}) error, function '{funcName}' already defined in this scope.");
            }

            string[] _params = new string[idtfs.Length - 1];

            for (int i = 1; i < idtfs.Length; i++)
            {
                _params[i - 1] = idtfs[i].GetText();
            }

            var block = context.block();
            NativeFuncts.Add(funcName, new NativeFuncData(funcName, block, _params));
            return null;
        }

        public override object VisitFunctionCall([NotNull] GScriptParser.FunctionCallContext context)
        {
            var ident = context.IDENTIFIER().GetText();
            if (ExternalFuncts.ContainsKey(ident))
            {
                return RunExternalFunction(context);
            }
            else if (NativeFuncts.ContainsKey(ident))
            {
                Memory.EnterBlock();
                NativeFuncts.EnterBlock();
                ParamMemory.EnterBlock();
                var ret = RunNativeFunction(context);
                Memory.ExitBlock();
                NativeFuncts.ExitBlock();
                ParamMemory.ExitBlock();
                return ret;
            }
            throw new Exception($"(line {context.Start.Line}) error, function {ident} not found.");
        }

        private object RunExternalFunction(GScriptParser.FunctionCallContext context)
        {
            var ident = context.IDENTIFIER().GetText();
            int numExpr = (context.children.Count - 2) / 2;
            object[] _params = new object[numExpr];
            for (int i = 0; i < _params.Length; i++)
            {
                object? ret = Visit(context.children[i * 2 + 2]);
                if (ret == null)
                    throw new Exception($"(line {context.Start.Line}) error, parameter at index {i} evaluated to null");
                _params[i] = (object)ret;
            }

            return ExternalFuncts[ident].Invoke(_params);
        }

        private object RunNativeFunction(GScriptParser.FunctionCallContext context)
        {
            var ident = context.IDENTIFIER().GetText();
            var expressions = context.expression();
            NativeFuncData funcData = NativeFuncts[ident];

            if (expressions.Length != funcData.ParamNames.Length)
            {
                throw new Exception($"(line {context.Start.Line}) error, expected {funcData.ParamNames.Length} parameter(s) but got {expressions.Length}");
            }

            for (int i = 0; i < funcData.ParamNames.Length; i++)
            {
                var value = Visit(expressions[i]);
                ParamMemory.Add(funcData.ParamNames[i], value);
            }

            return Visit(funcData.Block);
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

            throw new Exception($"error, can't decide truthiness of value {value} (type {value.GetType()})");
        }
    }
}

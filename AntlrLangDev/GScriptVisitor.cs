
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace AntlrLangDev
{

    internal class GScriptVisitor : GScriptBaseVisitor<object?>
    {
        public readonly StackDict<VariableData> Memory = new();

        private readonly Dictionary<string, Func<object[], object?>> ExternalFuncts = new();
        private readonly StackDict<NativeFuncData> NativeFuncts = new();
        private readonly StackDict<VariableData> ParamMemory = new();
        private readonly Stack<NativeFuncData> FunctionCallStack = new();

        private bool functionReturned = false;
        private object? funcReturnData;

        private Random rand = new Random();

        public GScriptVisitor()
        {
            ExternalFuncts.Add("print", PrintOp);
            ExternalFuncts.Add("rand", RandomOp);
            ExternalFuncts.Add("length", LengthOp);
        }

        private object? PrintOp(object[] args)
        {
            Console.WriteLine(args[0].ToString());
            return null;
        }

        private object LengthOp(object[] args)
        {
            if (args.Length != 1)
                throw new Exception($"error, expected one argument, got {args.Length}");
            if (args[0] is string s)
            {
                return s.Length;
            }
            else
            {
                return 1;
            }
        }

        private object RandomOp(object[] args)
        {
            return (float)rand.NextDouble();
        }

        protected override bool ShouldVisitNextChild(IRuleNode node, object? currentResult)
        {
            return !functionReturned;
        }

        public override object? VisitDeclaration([NotNull] GScriptParser.DeclarationContext context)
        {
            string identifier = context.IDENTIFIER().GetText();
            if (Memory.ContainsKey(identifier))
            {
                throw new Exception($"error, variable of name {identifier} was alrady declared.");
            }
            Type type = TypeFromString(context.type().GetText());
            bool isConst = context.@const() != null;

            var expression = context.expression();
            if (expression == null)
            {
                if (isConst)
                {
                    throw new Exception($"error, const variable declaration needs a value (line {context.Start.Line})");
                }
                Memory[identifier] = new VariableData(identifier, type, DefaultValueFromType(type));
                return null;
            }
            object? result = Visit(expression);
            if (result == null)
            {
                throw new Exception($"error, assignment expression evaluated to null");
            }
            if (type == typeof(float) && result.GetType() == typeof(int))
            {
                result = Convert.ToSingle(result);
            }
            if (result.GetType() != type)
            {
                throw new Exception($"error, trying to assign value of type '{result.GetType()}' to variable of type '{type}' (line {context.Start.Line})");
            }

            Memory[identifier] = new VariableData(identifier, type, result, isConst);
            return null;
        }

        public override object? VisitAssignment([NotNull] GScriptParser.AssignmentContext context)
        {
            string name = context.IDENTIFIER().GetText();
            object? value = Visit(context.expression());
            if (value == null)
                throw new Exception($"(line {context.Start.Line}) error, variable {name} is null");

            var scope = context.scope();
            var operation = context.assignOp().GetText();

            bool isVariable = scope != null && scope.GetText() == "global" //global variable
            || !ParamMemory.ContainsKey(name); //not in param list

            if (!Memory.ContainsKey(name))
            {
                throw new Exception($"error, variable '{name}' not declared (line {context.Start.Line})");
            }

            VariableData variable = isVariable ? Memory[name] : ParamMemory[name];

            if (variable.Constant)
            {
                throw new Exception($"error, illegal value assignment of const variable '{variable.Identifier}' (line {context.Start.Line})");
            }

            if (operation == "=")
            {
                if (variable.Type == value.GetType())
                {
                    variable.Data = value;
                }
                else if (variable.Type == typeof(float) && value.GetType() == typeof(int))
                {
                    variable.Data = Convert.ToSingle(value);
                }
                else
                {
                    throw new Exception($"error, cannot assign value of type '{value.GetType()}' to variable of type '{variable.Type}' (line {context.Start.Line})");
                }
                return null;
            }

            if (variable.Data is string s)
            {
                if (operation == "+=")
                {
                    variable.Data = s += value;
                }
                else
                {
                    throw new Exception($"(line {context.Start.Line}) error, operation '{operation}' not defined for strings.");
                }
            }
            else if (variable.Data is float f)
            {
                float operand = Convert.ToSingle(value);
                if (operation == "+=")
                {
                    f += operand;
                }
                else if (operation == "-=")
                {
                    f -= operand;
                }
                else
                {
                    throw new Exception($"(line {context.Start.Line}) error, operation '{operation}' not defined for floats.");
                }
                variable.Data = f;
            }
            else if (variable.Data is int i)
            {
                int operand = Convert.ToInt32(value);
                if (operation == "+=")
                {
                    i += operand;
                }
                else if (operation == "-=")
                {
                    i -= operand;
                }
                else
                {
                    throw new Exception($"(line {context.Start.Line}) error, operation '{operation}' not defined for floats.");
                }
                variable.Data = i;
            }
            else
            {
                throw new Exception($"(line {context.Start.Line}) error, assignment for variable of type '{variable.Data.GetType()}'.");
            }
            return null;
        }

        public override object? VisitConstantExpression([NotNull] GScriptParser.ConstantExpressionContext context)
        {
            return Visit(context.constant());
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
                return Memory[varname].Data;
            }
            else
            {
                if (ParamMemory.ContainsKey(varname))
                {
                    return ParamMemory[varname].Data;
                }
                else if (Memory.ContainsKey(varname))
                {
                    return Memory[varname].Data;
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
                throw new Exception($"(line {context.Start.Line}) error, typecast not defined for null type.");
            }
            Type varType = var.GetType();

            if (targetType == "string")
            {
                return var.ToString() ?? "";
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

        public override object VisitMultExpression([NotNull] GScriptParser.MultExpressionContext context)
        {
            var expressions = context.expression();
            var op = context.multOp();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            if (res1 == null || res2 == null)
            {
                throw new Exception($"(line {context.Start.Line}) error, mult operation not defined for null type.");
            }

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
                        return Convert.ToSingle(Convert.ToDouble(res1) + Convert.ToDouble(res2));
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

            if (res1 == null || res2 == null)
            {
                throw new Exception($"(line {context.Start.Line}) error, compare operation not defined for null type.");
            }

            var type1 = res1.GetType();
            var type2 = res2.GetType();
            var op = context.compareOp().GetText();

            if (type1 == typeof(string) || type2 == typeof(string))
            {
                if (type1 != typeof(string) || type2 != typeof(string))
                {
                    throw new Exception($"(line {context.Start.Line}) error, can't compare string to non-string.");
                }
                switch (op)
                {
                    case "==":
                        return (string)res1 == (string)res2;
                    case "!=":
                        return (string)res1 != (string)res2;
                    default:
                        throw new Exception($"(line {context.Start.Line}) error, invalid operation for string comparison: {op}.");
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
                        throw new Exception($"(line {context.Start.Line}) error, invalid operation for bool-ish comparison: {op}.");
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

            if (res1 == null || res2 == null)
            {
                throw new Exception($"(line {context.Start.Line}) error, and operation not defined for null type.");
            }

            if (res1.GetType() != typeof(bool) || res2.GetType() != typeof(bool))
            {
                throw new Exception($"(line {context.Start.Line}) error, non-boolean value used for bool operator.");
            }
            return (bool)res1 & (bool)res2;
        }

        public override object VisitOrExpression([NotNull] GScriptParser.OrExpressionContext context)
        {
            var expressions = context.expression();

            var res1 = Visit(expressions[0]);
            var res2 = Visit(expressions[1]);

            if (res1 == null || res2 == null)
            {
                throw new Exception($"(line {context.Start.Line}) error, or operation not defined for null type.");
            }

            if (res1.GetType() != typeof(bool) || res2.GetType() != typeof(bool))
            {
                throw new Exception($"(line {context.Start.Line}) error, non-boolean value used for bool operator.");
            }
            return (bool)res1 | (bool)res2;
        }

        public override object? VisitNullCoalescingExpression([NotNull] GScriptParser.NullCoalescingExpressionContext context)
        {
            var expr = context.expression();
            return Visit(expr[0]) ?? Visit(expr[1]);
        }

        public override object VisitUnaryExpression([NotNull] GScriptParser.UnaryExpressionContext context)
        {
            object? value = Visit(context.expression());

            if (value == null)
            {
                throw new Exception($"(line {context.Start.Line}) error, cannot operate on null.");
            }
            var unaryOp = context.unaryOp().GetText();
            if (unaryOp == "!")
            {
                return !IsTruthy(value);
            }
            if (unaryOp == "-")
            {
                if (value is int i)
                {
                    return -i;
                }
                if (value is float f)
                {
                    return -f;
                }
            }
            throw new Exception($"(line {context.Start.Line}) error, unary operator '{unaryOp}' not recognized.");
        }

        public override object VisitEnclosedExpression([NotNull] GScriptParser.EnclosedExpressionContext context)
        {
            object? res = Visit(context.expression());
            if (res == null)
            {
                throw new Exception($"(line {context.Start.Line}) error, enclosed expression evaluated to null.");
            }

            return res;
        }

        public override object? VisitWhileBlock([NotNull] GScriptParser.WhileBlockContext context)
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

        public override object? VisitIfBlock([NotNull] GScriptParser.IfBlockContext context)
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

        public override object? VisitFunctionDefinition([NotNull] GScriptParser.FunctionDefinitionContext context)
        {
            var idtfs = context.IDENTIFIER();
            var types = context.type();
            string funcName = idtfs[0].GetText();

            if (ExternalFuncts.ContainsKey(funcName))
            {
                throw new Exception($"(line {context.Start.Line}) error, can't reuse external function name '{funcName}'.");
            }
            if (NativeFuncts.Peek().ContainsKey(funcName))
            {
                throw new Exception($"(line {context.Start.Line}) error, function '{funcName}' already defined in this scope.");
            }

            string[] _params = new string[idtfs.Length - 1];
            Type[] _types = new Type[types.Length];

            for (int i = 1; i < idtfs.Length; i++)
            {
                _params[i - 1] = idtfs[i].GetText();
            }
            for (int i = 0; i < types.Length; i++)
            {
                _types[i] = TypeFromString(types[i].GetText());
            }

            var block = context.block();
            Type returnType = TypeFromString(context.funcReturnType().GetText());
            NativeFuncts.Add(funcName, new NativeFuncData(funcName, block, _params, _types, returnType));
            return null;
        }

        public override object? VisitFunctionCall([NotNull] GScriptParser.FunctionCallContext context)
        {
            funcReturnData = null;
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

                ValidateFunctionReturnType(NativeFuncts[ident].ReturnType, context.Start.Line);

                Memory.ExitBlock();
                NativeFuncts.ExitBlock();
                ParamMemory.ExitBlock();
                return ret;
            }
            throw new Exception($"(line {context.Start.Line}) error, function {ident} not found.");
        }

        public override object? VisitReturnStatement([NotNull] GScriptParser.ReturnStatementContext context)
        {
            var expr = context.expression();
            if (expr != null)
            {
                funcReturnData = Visit(expr);
            }
            else
            {
                funcReturnData = null;
            }
            functionReturned = true;

            NativeFuncData funcData = FunctionCallStack.Peek();

            if(funcReturnData != null && funcData.ReturnType == typeof(float) && funcReturnData.GetType() == typeof(int)){
                funcReturnData = Convert.ToSingle(funcReturnData);
            }

            // validate return statement type
            ValidateFunctionReturnType(funcData.ReturnType, context.Start.Line);

            return null;
        }

        private object? RunExternalFunction(GScriptParser.FunctionCallContext context)
        {
            var ident = context.IDENTIFIER().GetText();
            int numExpr = (context.children.Count - 2) / 2;
            object[] _params = new object[numExpr];
            for (int i = 0; i < _params.Length; i++)
            {
                object? ret = Visit(context.children[i * 2 + 2]);
                if (ret == null)
                    throw new Exception($"(line {context.Start.Line}) error, parameter at index {i} evaluated to null.");
                _params[i] = (object)ret;
            }

            return ExternalFuncts[ident].Invoke(_params);
        }

        private object? RunNativeFunction(GScriptParser.FunctionCallContext context)
        {
            var ident = context.IDENTIFIER().GetText();
            var expressions = context.expression();
            NativeFuncData funcData = NativeFuncts[ident];

            if (expressions.Length != funcData.ParamNames.Length)
            {
                throw new Exception($"(line {context.Start.Line}) error, expected {funcData.ParamNames.Length} parameter(s) but got {expressions.Length}.");
            }

            for (int i = 0; i < funcData.ParamNames.Length; i++)
            {
                object? value = Visit(expressions[i]);
                if (value == null)
                {
                    throw new Exception($"(line {context.Start.Line}) error, function parameter {funcData.ParamNames[i]} is null.");
                }
                VariableData paramData = new VariableData(funcData.ParamNames[i], funcData.ParamTypes[i], value);
                ParamMemory.Add(funcData.ParamNames[i], paramData);
            }
            FunctionCallStack.Push(funcData);
            Visit(funcData.Block);
            FunctionCallStack.Pop();

            functionReturned = false;
            if (funcData.ReturnType == typeof(void))
            {
                funcReturnData = null;
            }
            return funcReturnData;
        }

        private bool IsTruthy(object? value)
        {
            if (value == null)
            {
                throw new Exception($"error, truthiness of null not defined.");
            }

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

            throw new Exception($"error, can't decide truthiness of value {value} (type {value.GetType()}).");
        }

        private Type TypeFromString(string name)
        {
            switch (name)
            {
                case "int": return typeof(int);
                case "float": return typeof(float);
                case "string": return typeof(string);
                case "bool": return typeof(bool);
                case "void": return typeof(void);
                default: throw new Exception($"error, unknown typename '{name}'.");
            }
        }

        private object DefaultValueFromType(Type type)
        {
            if (type == typeof(int)) return 0;
            else if (type == typeof(float)) return 0f;
            else if (type == typeof(string)) return "";
            else if (type == typeof(bool)) return false;
            throw new Exception($"error, type '{type}' not recognised");
        }

        private void ValidateFunctionReturnType(Type expectedType, int line)
        {
            bool returnValid = funcReturnData == null && expectedType == typeof(void) || (funcReturnData != null && expectedType == funcReturnData.GetType());

            if (!returnValid)
            {
                Type receivedType = funcReturnData == null ? typeof(void) : funcReturnData.GetType();

                throw new Exception($"error, invalid return type. expected '{expectedType}', got '{receivedType}' (line {line})");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AntlrLangDev
{
    public static class Validation
    {
        public static void ValidateBoolOperands(int line, params object?[] operands)
        {
            foreach (object? operand in operands)
            {
                if (operand == null)
                {
                    throw new Exception($"error, operation not defined for null value (line {line})");
                }

                if (operand.GetType() != typeof(bool))
                {
                    throw new Exception($"error, non-boolean value used for bool operation (line {line})");
                }
            }
        }

        public static void ValidateNotNull(int line, object? obj){
            if (obj == null)
            {
                throw new Exception($"(line {line}) error, enclosed expression evaluated to null.");
            }
        }
    }
}
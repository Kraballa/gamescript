namespace AntlrLangDev
{
    internal class NativeFuncData
    {
        public string Identifier;
        public GScriptParser.BlockContext Block;
        public string[] ParamNames;
        public Type[] ParamTypes;

        public NativeFuncData(string identifier, GScriptParser.BlockContext block, string[] paramNames, Type[] paramTypes)
        {
            Identifier = identifier;
            Block = block;
            ParamNames = paramNames;
            ParamTypes = paramTypes;
        }
    }
}

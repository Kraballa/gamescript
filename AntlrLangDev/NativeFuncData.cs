namespace AntlrLangDev
{
    internal struct NativeFuncData
    {
        public string Identifier;
        public GScriptParser.FunctionBlockContext Block;
        public string[] ParamNames;
        
        public NativeFuncData(string identifier, GScriptParser.FunctionBlockContext block, string[] paramNames){
            Identifier = identifier;
            Block = block;
            ParamNames = paramNames;
        }

    }
}

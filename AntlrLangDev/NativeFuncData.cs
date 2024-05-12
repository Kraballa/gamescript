namespace AntlrLangDev
{
    internal struct NativeFuncData
    {
        public string Identifier;
        public GScriptParser.BlockContext Block;
        public string[] ParamNames;
        
        public NativeFuncData(string identifier, GScriptParser.BlockContext block, string[] paramNames){
            Identifier = identifier;
            Block = block;
            ParamNames = paramNames;
        }

    }
}

namespace AntlrLangDev
{
    internal struct NativeFuncData
    {
        public GScriptParser.BlockContext Block;
        public string[] ParamNames;
        
        public NativeFuncData(GScriptParser.BlockContext block, string[] paramNames){
            Block = block;
            ParamNames = paramNames;
        }

    }
}

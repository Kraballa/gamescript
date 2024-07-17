namespace AntlrLangDev
{
    internal class VariableData
    {
        public string Identifier;
        public Type Type;
        public object Data;
        public bool Constant = false;

        public VariableData(string identifier, Type type, object data, bool constant = false)
        {
            Identifier = identifier;
            Type = type;
            Data = data;
            Constant = constant;
        }
    }
}

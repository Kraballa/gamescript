namespace AntlrLangDev
{
    internal class VariableData
    {
        public string Identifier;
        public Type Type;
        public object Data;

        public VariableData(string identifier, Type type, object data)
        {
            Identifier = identifier;
            Type = type;
            Data = data;
        }
    }
}

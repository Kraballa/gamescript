namespace AntlrLangDev
{
    internal struct VariableData
    {
        string Identifier;
        Type Type;
        object Data;

        public VariableData(string identifier, Type type, object data)
        {
            Identifier = identifier;
            Type = type;
            Data = data;
        }
    }

}

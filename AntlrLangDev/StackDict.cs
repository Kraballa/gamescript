
using System.ComponentModel;

namespace AntlrLangDev;

/// <summary>
/// Stackable dictionary for scoping data to blocks.
/// </summary>
/// <typeparam name="TValue">Value type</typeparam>
internal class StackDict<TValue>
{

    private Stack<Dictionary<string, TValue>> VariableStack = new();

    public StackDict(Dictionary<string, TValue> memory)
    {
        VariableStack.Push(memory);
    }

    public StackDict() : this(new Dictionary<string, TValue>())
    {

    }

    public TValue this[string key]
    {
        get => Get(key);
        set => Add(key, value);
    }

    public bool ContainsKey(string identifier)
    {
        foreach (var dict in VariableStack)
        {
            if (dict.ContainsKey(identifier))
            {
                return true;
            }
        }
        return false;
    }

    public TValue Get(string identifier)
    {
        foreach (var dict in VariableStack)
        {
            if (dict.ContainsKey(identifier))
            {
                return dict[identifier];
            }
        }
        throw new Exception($"error, key {identifier} not found");
    }

    public void Add(string identifier, TValue value)
    {
        foreach (var dict in VariableStack)
        {
            if (dict.ContainsKey(identifier))
            {
                dict[identifier] = value;
                return;
            }
        }

        VariableStack.Peek()[identifier] = value;
    }

    public void EnterBlock()
    {
        VariableStack.Push(new Dictionary<string, TValue>());
    }

    public void ExitBlock()
    {
        VariableStack.Pop();
    }
}
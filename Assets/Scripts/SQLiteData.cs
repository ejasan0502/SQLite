using System.Data;

public struct SQLiteData
{
    public string name;
    public SQLiteDataType dataType;

    public SQLiteData(string name, SQLiteDataType dataType)
    {
        this.name = name;
        this.dataType = dataType;
    }
}
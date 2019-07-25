using Mono.Data.Sqlite;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class SQLiteHandler : MonoBehaviour
{
    [SerializeField] private string databaseName;

    private Dictionary<string, List<SQLiteData>> schemas;

    private string connectionString
    {
        get
        {
            return $"URI=file:{Application.persistentDataPath}/{databaseName}.db";
        }
    }

    private void Awake()
    {
        schemas = new Dictionary<string, List<SQLiteData>>();
    }
    private void Start()
    {
        CreateSchema("high_score", new SQLiteData("name", SQLiteDataType.TEXT), new SQLiteData("score", SQLiteDataType.INTEGER));
        Insert("high_score", "GG Meade", 3701);
        Insert("high_score", "US Grant", 4242);
        Insert("high_score", "GB McClellan", 107);
        DisplayValues("high_score", "score", 10);
    }

    private object Parse(SqliteDataReader reader, SQLiteDataType dataType, int index)
    {
        switch (dataType)
        {
            case SQLiteDataType.BLOB: break;
            case SQLiteDataType.INTEGER: return reader.GetInt32(index);
            case SQLiteDataType.REAL: return reader.GetFloat(index);
            case SQLiteDataType.TEXT: return reader.GetString(index);
        }

        return null;
    }

    public void CreateSchema(string tableName, params SQLiteData[] variables)
    {
        if (schemas.ContainsKey(tableName))
        {
            Debug.LogWarning($"WARNING: {tableName} already exists!");
            return;
        }

        using (var conn = new SqliteConnection(connectionString))
        {
            
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = $"CREATE TABLE IF NOT EXISTS '{tableName}' ( ";

                if (!schemas.ContainsKey(tableName))
                {
                    schemas.Add(tableName, new List<SQLiteData>());
                }
                for (int i = 0; i < variables.Length; i++)
                {
                    if (i != 0) cmd.CommandText += ",";
                    cmd.CommandText += $"  '{variables[i].name}' {variables[i].dataType.ToString()} NOT NULL";
                    schemas[tableName].Add(variables[i]);
                }
                
                cmd.CommandText += ");";

                var result = cmd.ExecuteNonQuery();
                Debug.Log("Created schema: " + result);
            }
        }
    }

    public void Insert(string tableName, params object[] values)
    {
        if (!schemas.ContainsKey(tableName))
        {
            Debug.LogWarning($"WARNING: {tableName} does not exist!");
            return;
        }
        if (schemas[tableName].Count != values.Length)
        {
            Debug.LogError($"ERROR: {tableName} has {values.Length} variables! You must pass the same amount of variables!");
            return;
        }

        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;

                string variablesText = "(";
                string valuesText = "(";
                for (int i = 0; i < schemas[tableName].Count; i++)
                {
                    if (i != 0) {
                        variablesText += ", ";
                        valuesText += ", ";
                    }

                    variablesText += schemas[tableName][i].name;
                    valuesText += "@" + schemas[tableName][i].name;

                    cmd.Parameters.Add(new SqliteParameter
                    {
                        ParameterName = schemas[tableName][i].name,
                        Value = values[i]
                    });
                }
                variablesText += ")";
                valuesText += ")";

                cmd.CommandText = $"INSERT INTO {tableName} {variablesText} " +
                                  $"VALUES {valuesText};";

                var result = cmd.ExecuteNonQuery();
                Debug.Log("Insert: " + result);
            }
        }
    }

    public void DisplayValues(string tableName, string variableName, int limit)
    {
        if (!schemas.ContainsKey(tableName))
        {
            Debug.LogWarning($"WARNING: {tableName} does not exist!");
            return;
        }

        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = $"SELECT * FROM {tableName} ORDER BY {variableName} DESC LIMIT @Count;";

                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "Count",
                    Value = limit
                });

                Debug.Log("Values (begin)");
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    for (int i = 0; i < schemas[tableName].Count; i++)
                    {
                        object val = Parse(reader, schemas[tableName][i].dataType, i);
                        Debug.Log($"{schemas[tableName][i].name}: {val}");
                    }
                }
                Debug.Log("Values (end)");
            }
        }
    }
}
// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================
using UnityEngine;

#if UNITY_EDITOR
#endif

// =======================================================================================
// NetworkManagerMMO
// =======================================================================================
public partial class NetworkManagerMMO
{
    public enum DatabaseType { SQLite, mySQL }

    [Header("[-=-=- UCE DATABASE TYPE -=-=-]")]
    public DatabaseType databaseType = DatabaseType.SQLite;

    // uses Suriyun Editor tools to toggle visiblity of the following fields
    // those fields are only visible when mySQL is selected

    [StringShowConditional(conditionFieldName: "databaseType", conditionValue: "mySQL")]
    public string dbHost = "localhost";

    [StringShowConditional(conditionFieldName: "databaseType", conditionValue: "mySQL")]
    public string dbName = "dbName";

    [StringShowConditional(conditionFieldName: "databaseType", conditionValue: "mySQL")]
    public string dbUser = "dbUser";

    [StringShowConditional(conditionFieldName: "databaseType", conditionValue: "mySQL")]
    public string dbPassword = "dbPassword";

    [StringShowConditional(conditionFieldName: "databaseType", conditionValue: "mySQL")]
    public uint dbPort = 3306;

    [StringShowConditional(conditionFieldName: "databaseType", conditionValue: "mySQL")]
    public string dbCharacterSet = "utf8mb4";

    protected const string DB_SQLITE = "_SQLITE";
    protected const string DB_MYSQL = "_MYSQL";

    // -----------------------------------------------------------------------------------
    // OnValidate
    // -----------------------------------------------------------------------------------
    [DevExtMethods("OnValidate")]
    private void OnValidate_UCE_Tools()
    {
#if UNITY_EDITOR
        if (databaseType == NetworkManagerMMO.DatabaseType.SQLite)
        {
            UCE_EditorTools.RemoveScriptingDefine(DB_MYSQL);
            UCE_EditorTools.AddScriptingDefine(DB_SQLITE);
        }
        else if (databaseType == NetworkManagerMMO.DatabaseType.mySQL)
        {
            UCE_EditorTools.RemoveScriptingDefine(DB_SQLITE);
            UCE_EditorTools.AddScriptingDefine(DB_MYSQL);
        }
#endif
    }
    
    // -----------------------------------------------------------------------------------
}
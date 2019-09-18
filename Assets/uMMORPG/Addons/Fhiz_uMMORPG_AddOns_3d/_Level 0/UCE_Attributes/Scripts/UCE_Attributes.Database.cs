// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
#if _MYSQL
using MySql.Data;								// From MySql.Data.dll in Plugins folder
using MySql.Data.MySqlClient;                   // From MySql.Data.dll in Plugins folder
#elif _SQLITE
using Mono.Data.Sqlite; 						// copied from Unity/Mono/lib/mono/2.0 to Plugins
#endif

// =======================================================================================
// DATABASE (SQLite / mySQL Hybrid)
// =======================================================================================
public partial class Database {

	// -----------------------------------------------------------------------------------
    // Connect_UCE_Attributes
    // -----------------------------------------------------------------------------------
    [DevExtMethods("Connect")]
    private void Connect_UCE_Attributes() {
#if _MYSQL && _FHIZATTRIBUTES
		ExecuteNonQueryMySql(@"CREATE TABLE IF NOT EXISTS UCE_attributes (
                        `character` VARCHAR(32) NOT NULL,
                        slot INTEGER NOT NULL,
                        name TEXT NOT NULL,
                        points INTEGER NOT NULL
                        ) CHARACTER SET=utf8mb4");
#elif _SQLITE && _FHIZATTRIBUTES
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_UCE_attributes (
                        character TEXT NOT NULL,
                        slot INTEGER NOT NULL,
                        name TEXT NOT NULL,
                        points INTEGER NOT NULL)");
#endif
    }
    
	// -----------------------------------------------------------------------------------
    // CharacterLoad_UCE_Attributes
    // -----------------------------------------------------------------------------------
    [DevExtMethods("CharacterLoad")]
    private void CharacterLoad_UCE_Attributes(Player player) {
    
#if _MYSQL && _FHIZATTRIBUTES
		foreach (UCE_AttributeTemplate template in player.playerAttributes.UCE_AttributeTypes) {
            if (template == null) continue;
            UCE_Attribute attr = new UCE_Attribute(template);
            var table = ExecuteReaderMySql("SELECT points FROM UCE_attributes WHERE `character`=@character AND name=@name", new MySqlParameter("@character", player.name), new MySqlParameter("@name", attr.name));
            if (table.Count == 1) {
                var row = table[0];
                attr.points = (int)row[0];
            }
            player.UCE_Attributes.Add(attr);
        }
#elif _SQLITE && _FHIZATTRIBUTES
        foreach (UCE_AttributeTemplate template in player.playerAttributes.UCE_AttributeTypes) {
            if (template == null) continue;
            UCE_Attribute attr = new UCE_Attribute(template);
            var table = ExecuteReader("SELECT points FROM character_UCE_attributes WHERE character=@character AND name=@name", new SqliteParameter("@character", player.name), new SqliteParameter("@name", attr.name));
            if (table.Count == 1) {
                var row = table[0];
                attr.points = Convert.ToInt32((long)row[0]);
            }
            player.UCE_Attributes.Add(attr);
        }
#endif
	
    }
    
	// -----------------------------------------------------------------------------------
    // CharacterSave_UCE_Attributes
    // -----------------------------------------------------------------------------------
    [DevExtMethods("CharacterSave")]
    private void CharacterSave_UCE_Attributes(Player player) {

#if _MYSQL && _FHIZATTRIBUTES
		ExecuteNonQueryMySql("DELETE FROM UCE_attributes WHERE `character`=@character", new MySqlParameter("@character", player.name));
        for (int i = 0; i < player.UCE_Attributes.Count; ++i) {
            var attr = player.UCE_Attributes[i];
            ExecuteNonQueryMySql("INSERT INTO UCE_attributes VALUES (@character, @slot, @name, @points)",
                            new MySqlParameter("@character", player.name),
                            new MySqlParameter("@slot", i),
                            new MySqlParameter("@name", attr.name),
                            new MySqlParameter("@points", attr.points));
        }
#elif _SQLITE && _FHIZATTRIBUTES
        ExecuteNonQuery("DELETE FROM character_UCE_attributes WHERE character=@character", new SqliteParameter("@character", player.name));
        for (int i = 0; i < player.UCE_Attributes.Count; ++i) {
            var attr = player.UCE_Attributes[i];
            ExecuteNonQuery("INSERT INTO character_UCE_attributes VALUES (@character, @slot, @name, @points)",
                           new SqliteParameter("@character", player.name),
                           new SqliteParameter("@slot", i),
                           new SqliteParameter("@name", attr.name),
                           new SqliteParameter("@points", attr.points));
        }
#endif
    }
    
    // -----------------------------------------------------------------------------------
    
}

// =======================================================================================

// =======================================================================================
// ADVANCED HARVESTING - DATABASE
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using Mono.Data.Sqlite; // copied from Unity/Mono/lib/mono/2.0 to Plugins
using System;

#if _AdvancedHarvesting

// =======================================================================================
// DATABASE
// =======================================================================================
public partial class Database {

	// -----------------------------------------------------------------------------------
	// Initialize_UMO3d_AdvancedHarvesting
	// -----------------------------------------------------------------------------------
    static void Initialize_UMO3d_AdvancedHarvesting() {
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS UMO3d_character_professions ( character TEXT NOT NULL, profession TEXT NOT NULL, experience INTEGER)");
    }
    
	// -----------------------------------------------------------------------------------
	// CharacterLoad_UMO3d_AdvancedHarvesting
	// -----------------------------------------------------------------------------------
    public static void CharacterLoad_UMO3d_AdvancedHarvesting(Player player) {
        var table = ExecuteReader("SELECT profession, experience FROM UMO3d_character_professions WHERE character=@character", new SqliteParameter("@character", player.name));

        foreach (var row in table) {
            var profession = new AdvancedHarvestingProfession((string)row[0]);
            profession.experience = Convert.ToInt32((long)row[1]);
            player.UMO3d_Professions.Add(profession);
        }
    }
    
	// -----------------------------------------------------------------------------------
	// CharacterSave_UMO3d_AdvancedHarvesting
	// -----------------------------------------------------------------------------------
    public static void CharacterSave_UMO3d_AdvancedHarvesting(Player player) {
        ExecuteNonQuery("DELETE FROM UMO3d_character_professions WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (var profession in player.UMO3d_Professions)
            ExecuteNonQuery("INSERT INTO UMO3d_character_professions VALUES (@character, @profession, @experience)",
                            new SqliteParameter("@character", player.name),
                            new SqliteParameter("@profession", profession.templateName),
                            new SqliteParameter("@experience", profession.experience));
    }
    
    // -----------------------------------------------------------------------------------
    
}

#endif

// =======================================================================================

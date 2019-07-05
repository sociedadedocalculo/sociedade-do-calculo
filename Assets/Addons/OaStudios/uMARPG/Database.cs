using Mono.Data.Sqlite;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public partial class Database
{

    static void Initialize_UMARPG()
    {
        try
        {
            // create tables if they don't exist yet or were deleted
            ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_uma_details (
                            name TEXT NOT NULL PRIMARY KEY,
                            account TEXT NOT NULL,
                            dna TEXT NOT NULL DEFAULT 'N',
                            gender TINYINT NOT NULL DEFAULT 0,
                            hair_index TINYINT NOT NULL DEFAULT 0,
                            beard_index TINYINT NOT NULL DEFAULT 0,
                            skin_color TINYINT NOT NULL DEFAULT 0,
                            hair_color TINYINT NOT NULL DEFAULT 0
                            )");

        }
        catch (SqliteException ex)
        {
            // field already exists. Do nothing.
            Debug.Log(ex.ToString());
        }
    }

    static void CharacterSave_UMARPG(Player player)
    {
        byte[] dna = new byte[player.dna.Count];
        for (int i = 0; i < dna.Length; i++)
            dna[i] = player.dna[i].value;

        string dnaString = Encoding.UTF8.GetString(dna);

        ExecuteNonQuery("INSERT OR REPLACE INTO character_uma_details VALUES(@name, @account, @dna, @gender, @hair_index, @beard_index, @skin_color, @hair_color)",
            new SqliteParameter("@name",        player.name),
            new SqliteParameter("@account",     player.account), 
            new SqliteParameter("@dna",         dnaString),
            new SqliteParameter("@gender",      player.gender),
            new SqliteParameter("@hair_index",  player.umaSyncData[0].value),
            new SqliteParameter("@beard_index", player.umaSyncData[1].value),
            new SqliteParameter("@skin_color",  player.umaSyncData[2].value),
            new SqliteParameter("@hair_color",  player.umaSyncData[3].value));
    }

    static void CharacterLoad_UMARPG(Player player)
    {
        var table = ExecuteReader("SELECT dna, gender, hair_index, beard_index, skin_color, hair_color FROM character_uma_details WHERE account=@account AND name=@character",
            new SqliteParameter("@account", player.account),
            new SqliteParameter("@character", player.name));

        foreach (var row in table)
        {
            byte[] dna = Encoding.UTF8.GetBytes((string)row[0]);
            foreach (byte b in dna)
                player.dna.Add(new Dna() { value = b });

            int gender = int.Parse(row[1].ToString());
            if (gender == 1)
                player.gender = true;
            else
                player.gender = false;

            player.umaSyncData.Add(new UmaData() { value = byte.Parse(row[2].ToString()) }); // Hair Index
            player.umaSyncData.Add(new UmaData() { value = byte.Parse(row[3].ToString()) }); // Beard Index
            player.umaSyncData.Add(new UmaData() { value = byte.Parse(row[4].ToString()) }); // Skin Color
            player.umaSyncData.Add(new UmaData() { value = byte.Parse(row[5].ToString()) }); // Hair Color
        }
    }

}

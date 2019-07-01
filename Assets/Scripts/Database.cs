// Saves Character Data in a SQLite database. We use SQLite for serveral reasons
//
// - SQLite is file based and works without having to setup a database server
//   - We can 'remove all ...' or 'modify all ...' easily via SQL queries
//   - A lot of people requested a SQL database and weren't comfortable with XML
//   - We can allow all kinds of character names, even chinese ones without
//     breaking the file system.
// - We will need MYSQL or similar when using multiple server instances later
//   and upgrading is trivial
// - XML is easier, but:
//   - we can't easily read 'just the class of a character' etc., but we need it
//     for character selection etc. often
//   - if each account is a folder that contains players, then we can't save
//     additional account info like password, banned, etc. unless we use an
//     additional account.xml file, which overcomplicates everything
//   - there will always be forbidden file names like 'COM', which will cause
//     problems when people try to create accounts or characters with that name
//
// About item mall coins:
//   The payment provider's callback should add new orders to the
//   character_orders table. The server will then process them while the player
//   is ingame. Don't try to modify 'coins' in the character table directly.
//
// Tools to open sqlite database files:
//   Windows/OSX program: http://sqlitebrowser.org/
//   Firefox extension: https://addons.mozilla.org/de/firefox/addon/sqlite-manager/
//   Webhost: Adminer/PhpLiteAdmin
//
// About performance:
// - It's recommended to only keep the SQlite connection open while it's used.
//   MMO Servers use it all the time, so we keep it open all the time. This also
//   allows us to use transactions easily, and it will make the transition to
//   MYSQL easier.
// - Transactions are definitely necessary:
//   saving 100 players without transactions takes 3.6s
//   saving 100 players with transactions takes    0.38s
// - Using tr = conn.BeginTransaction() + tr.Commit() and passing it through all
//   the functions is ultra complicated. We use a BEGIN + END queries instead.
//
// Some benchmarks:
//   saving 100 players unoptimized: 4s
//   saving 100 players always open connection + transactions: 3.6s
//   saving 100 players always open connection + transactions + WAL: 3.6s
//   saving 100 players in 1 'using tr = ...' transaction: 380ms
//   saving 100 players in 1 BEGIN/END style transactions: 380ms
//   saving 100 players with XML: 369ms
//
// Build notes:
// - requires Player settings to be set to '.NET' instead of '.NET Subset',
//   otherwise System.Data.dll causes ArgumentException.
// - requires sqlite3.dll x86 and x64 version for standalone (windows/mac/linux)
//   => found on sqlite.org website
// - requires libsqlite3.so x86 and armeabi-v7a for android
//   => compiled from sqlite.org amalgamation source with android ndk r9b linux
using UnityEngine;
using Mirror;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Mono.Data.Sqlite; // copied from Unity/Mono/lib/mono/2.0 to Plugins

public partial class Database
{
    // database path: Application.dataPath is always relative to the project,
    // but we don't want it inside the Assets folder in the Editor (git etc.),
    // instead we put it above that.
    // we also use Path.Combine for platform independent paths
    // and we need persistentDataPath on android
#if UNITY_EDITOR
    static string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Database.sqlite");
#elif UNITY_ANDROID
    static string path = Path.Combine(Application.persistentDataPath, "Database.sqlite");
#elif UNITY_IOS
    static string path = Path.Combine(Application.persistentDataPath, "Database.sqlite");
#else
    static string path = Path.Combine(Application.dataPath, "Database.sqlite");
#endif

    static SqliteConnection connection;

    // constructor /////////////////////////////////////////////////////////////
    static Database()
    {
        // create database file if it doesn't exist yet
        if (!File.Exists(path))
            SqliteConnection.CreateFile(path);

        // open connection
        connection = new SqliteConnection("URI=file:" + path);
        connection.Open();

        // create tables if they don't exist yet or were deleted
        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS characters (
                            name TEXT NOT NULL PRIMARY KEY,
                            account TEXT NOT NULL,
                            class TEXT NOT NULL,
                            x REAL NOT NULL,
                            y REAL NOT NULL,
                            z REAL NOT NULL,
                            level INTEGER NOT NULL,
                            health INTEGER NOT NULL,
                            mana INTEGER NOT NULL,
                            strength INTEGER NOT NULL,
                            intelligence INTEGER NOT NULL,
                            experience INTEGER NOT NULL,
                            skillExperience INTEGER NOT NULL,
                            gold INTEGER NOT NULL,
                            coins INTEGER NOT NULL,
                            online TEXT NOT NULL,
                            deleted INTEGER NOT NULL)");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_inventory (
                            character TEXT NOT NULL,
                            slot INTEGER NOT NULL,
                            name TEXT NOT NULL,
                            amount INTEGER NOT NULL,
                            petHealth INTEGER NOT NULL,
                            petLevel INTEGER NOT NULL,
                            petExperience INTEGER NOT NULL,
                            PRIMARY KEY(character, slot))");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_equipment (
                            character TEXT NOT NULL,
                            slot INTEGER NOT NULL,
                            name TEXT NOT NULL,
                            amount INTEGER NOT NULL,
                            PRIMARY KEY(character, slot))");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_skills (
                            character TEXT NOT NULL,
                            name TEXT NOT NULL,
                            level INTEGER NOT NULL,
                            castTimeEnd REAL NOT NULL,
                            cooldownEnd REAL NOT NULL,
                            PRIMARY KEY(character, name))");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_buffs (
                            character TEXT NOT NULL,
                            name TEXT NOT NULL,
                            level INTEGER NOT NULL,
                            buffTimeEnd REAL NOT NULL,
                            PRIMARY KEY(character, name))");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_quests (
                            character TEXT NOT NULL,
                            name TEXT NOT NULL,
                            killed INTEGER NOT NULL,
                            completed INTEGER NOT NULL,
                            PRIMARY KEY(character, name))");

        // INTEGER PRIMARY KEY is auto incremented by sqlite if the
        // insert call passes NULL for it.
        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_orders (
                            orderid INTEGER PRIMARY KEY,
                            character TEXT NOT NULL,
                            coins INTEGER NOT NULL,
                            processed INTEGER NOT NULL)");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        // guild members are saved in a separate table because instead of in a
        // characters.guild field because:
        // * guilds need to be resaved independently, not just in CharacterSave
        // * kicked members' guilds are cleared automatically because we drop
        //   and then insert all members each time. otherwise we'd have to
        //   update the kicked member's guild field manually each time
        // * it's easier to remove / modify the guild feature if it's not hard-
        //   coded into the characters table
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS character_guild (
                            character TEXT NOT NULL PRIMARY KEY,
                            guild TEXT NOT NULL,
                            rank INTEGER NOT NULL)");

        // add index on guild to avoid full scans when loading guild members
        ExecuteNonQuery("CREATE INDEX IF NOT EXISTS character_guild_by_guild ON character_guild (guild)");

        // guild master is not in guild_info in case we need more than one later
        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS guild_info (
                            name TEXT NOT NULL PRIMARY KEY,
                            notice TEXT NOT NULL)");

        // [PRIMARY KEY is important for performance: O(log n) instead of O(n)]
        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS accounts (
                            name TEXT NOT NULL PRIMARY KEY,
                            password TEXT NOT NULL,
                            banned INTEGER NOT NULL)");

        // addon system hooks
        Utils.InvokeMany(typeof(Database), null, "Initialize_");

        Debug.Log("connected to database");
    }

    // helper functions ////////////////////////////////////////////////////////
    // run a query that doesn't return anything
    public static void ExecuteNonQuery(string sql, params SqliteParameter[] args)
    {
        using (SqliteCommand command = new SqliteCommand(sql, connection))
        {
            foreach (SqliteParameter param in args)
                command.Parameters.Add(param);
            command.ExecuteNonQuery();
        }
    }

    // run a query that returns a single value
    public static object ExecuteScalar(string sql, params SqliteParameter[] args)
    {
        using (SqliteCommand command = new SqliteCommand(sql, connection))
        {
            foreach (SqliteParameter param in args)
                command.Parameters.Add(param);
            return command.ExecuteScalar();
        }
    }

    // run a query that returns several values
    // note: sqlite has long instead of int, so use Convert.ToInt32 etc.
    public static List<List<object>> ExecuteReader(string sql, params SqliteParameter[] args)
    {
        List<List<object>> result = new List<List<object>>();

        using (SqliteCommand command = new SqliteCommand(sql, connection))
        {
            foreach (SqliteParameter param in args)
                command.Parameters.Add(param);

            using (SqliteDataReader reader = command.ExecuteReader())
            {
                // the following code causes a SQL EntryPointNotFoundException
                // because sqlite3_column_origin_name isn't found on OSX and
                // some other platforms. newer mono versions have a workaround,
                // but as long as Unity doesn't update, we will have to work
                // around it manually. see also GetSchemaTable function:
                // https://github.com/mono/mono/blob/master/mcs/class/Mono.Data.Sqlite/Mono.Data.Sqlite_2.0/SQLiteDataReader.cs
                //
                //result.Load(reader); (DataTable)
                while (reader.Read())
                {
                    object[] buffer = new object[reader.FieldCount];
                    reader.GetValues(buffer);
                    result.Add(buffer.ToList());
                }
            }
        }

        return result;
    }

    // account data ////////////////////////////////////////////////////////////
    public static bool IsValidAccount(string account, string password)
    {
        // this function can be used to verify account credentials in a database
        // or a content management system.
        //
        // for example, we could setup a content management system with a forum,
        // news, shop etc. and then use a simple HTTP-GET to check the account
        // info, for example:
        //
        //   var request = new WWW("example.com/verify.php?id="+id+"&amp;pw="+pw);
        //   while (!request.isDone)
        //       print("loading...");
        //   return request.error == null && request.text == "ok";
        //
        // where verify.php is a script like this one:
        //   <?php
        //   // id and pw set with HTTP-GET?
        //   if (isset($_GET['id']) && isset($_GET['pw'])) {
        //       // validate id and pw by using the CMS, for example in Drupal:
        //       if (user_authenticate($_GET['id'], $_GET['pw']))
        //           echo "ok";
        //       else
        //           echo "invalid id or pw";
        //   }
        //   ?>
        //
        // or we could check in a MYSQL database:
        //   var dbConn = new MySql.Data.MySqlClient.MySqlConnection("Persist Security Info=False;server=localhost;database=notas;uid=root;password=" + dbpwd);
        //   var cmd = dbConn.CreateCommand();
        //   cmd.CommandText = "SELECT id FROM accounts WHERE id='" + account + "' AND pw='" + password + "'";
        //   dbConn.Open();
        //   var reader = cmd.ExecuteReader();
        //   if (reader.Read())
        //       return reader.ToString() == account;
        //   return false;
        //
        // as usual, we will use the simplest solution possible:
        // create account if not exists, compare password otherwise.
        // no CMS communication necessary and good enough for an Indie MMORPG.

        // not empty?
        if (!Utils.IsNullOrWhiteSpace(account) && !Utils.IsNullOrWhiteSpace(password))
        {
            List<List<object>> table = ExecuteReader("SELECT password, banned FROM accounts WHERE name=@name", new SqliteParameter("@name", account));
            if (table.Count == 1)
            {
                // account exists. check password and ban status.
                List<object> row = table[0];
                return (string)row[0] == password && (long)row[1] == 0;
            }
            else
            {
                // account doesn't exist. create it.
                ExecuteNonQuery("INSERT INTO accounts VALUES (@name, @password, 0)", new SqliteParameter("@name", account), new SqliteParameter("@password", password));
                return true;
            }
        }
        return false;
    }

    // character data //////////////////////////////////////////////////////////
    public static bool CharacterExists(string characterName)
    {
        // checks deleted ones too so we don't end up with duplicates if we un-
        // delete one
        return ((long)ExecuteScalar("SELECT Count(*) FROM characters WHERE name=@name", new SqliteParameter("@name", characterName))) == 1;
    }

    public static void CharacterDelete(string characterName)
    {
        // soft delete the character so it can always be restored later
        ExecuteNonQuery("UPDATE characters SET deleted=1 WHERE name=@character", new SqliteParameter("@character", characterName));
    }

    // returns the list of character names for that account
    // => all the other values can be read with CharacterLoad!
    public static List<string> CharactersForAccount(string account)
    {
        List<string> result = new List<string>();
        List<List<object>> table = ExecuteReader("SELECT name FROM characters WHERE account=@account AND deleted=0", new SqliteParameter("@account", account));
        foreach (List<object> row in table)
            result.Add((string)row[0]);
        return result;
    }

    static void LoadInventory(Player player)
    {
        // fill all slots first
        for (int i = 0; i < player.inventorySize; ++i)
            player.inventory.Add(new ItemSlot());

        // then load valid items and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        List<List<object>> table = ExecuteReader("SELECT name, slot, amount, petHealth, petLevel, petExperience FROM character_inventory WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (List<object> row in table)
        {
            string itemName = (string)row[0];
            int slot = Convert.ToInt32((long)row[1]);
            ScriptableItem itemData;
            if (slot < player.inventorySize && ScriptableItem.dict.TryGetValue(itemName.GetStableHashCode(), out itemData))
            {
                Item item = new Item(itemData);
                int amount = Convert.ToInt32((long)row[2]);
                item.petHealth = Convert.ToInt32((long)row[3]);
                item.petLevel = Convert.ToInt32((long)row[4]);
                item.petExperience = (long)row[5];
                player.inventory[slot] = new ItemSlot(item, amount); ;
            }
        }
    }

    static void LoadEquipment(Player player)
    {
        // fill all slots first
        for (int i = 0; i < player.equipmentInfo.Length; ++i)
            player.equipment.Add(new ItemSlot());

        // then load valid equipment and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        List<List<object>> table = ExecuteReader("SELECT name, slot, amount FROM character_equipment WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (List<object> row in table)
        {
            string itemName = (string)row[0];
            int slot = Convert.ToInt32((long)row[1]);
            ScriptableItem itemData;
            if (slot < player.equipmentInfo.Length && ScriptableItem.dict.TryGetValue(itemName.GetStableHashCode(), out itemData))
            {
                Item item = new Item(itemData);
                int amount = Convert.ToInt32((long)row[2]);
                player.equipment[slot] = new ItemSlot(item, amount);
            }
        }
    }

    static void LoadSkills(Player player)
    {
        // load skills based on skill templates (the others don't matter)
        // -> this way any skill changes in a prefab will be applied
        //    to all existing players every time (unlike item templates
        //    which are only for newly created characters)

        // fill all slots first
        foreach (ScriptableSkill skillData in player.skillTemplates)
            player.skills.Add(new Skill(skillData));

        // then load learned skills and put into their slots
        // (one big query is A LOT faster than querying each slot separately)
        List<List<object>> table = ExecuteReader("SELECT name, level, castTimeEnd, cooldownEnd FROM character_skills WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (List<object> row in table)
        {
            string skillName = (string)row[0];
            int index = player.skills.FindIndex(skill => skill.name == skillName);
            if (index != -1)
            {
                Skill skill = player.skills[index];
                // make sure that 1 <= level <= maxlevel (in case we removed a skill
                // level etc)
                skill.level = Mathf.Clamp(Convert.ToInt32((long)row[1]), 1, skill.maxLevel);
                // make sure that 1 <= level <= maxlevel (in case we removed a skill
                // level etc)
                // castTimeEnd and cooldownEnd are based on Time.time, which
                // will be different when restarting a server, hence why we
                // saved them as just the remaining times. so let's convert them
                // back again.
                skill.castTimeEnd = (float)row[2] + Time.time;
                skill.cooldownEnd = (float)row[3] + Time.time;

                player.skills[index] = skill;
            }
        }
    }

    static void LoadBuffs(Player player)
    {
        // load buffs
        // note: no check if we have learned the skill for that buff
        //       since buffs may come from other people too
        List<List<object>> table = ExecuteReader("SELECT name, level, buffTimeEnd FROM character_buffs WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (List<object> row in table)
        {
            string buffName = (string)row[0];
            ScriptableSkill skillData;
            if (ScriptableSkill.dict.TryGetValue(buffName.GetStableHashCode(), out skillData))
            {
                // make sure that 1 <= level <= maxlevel (in case we removed a skill
                // level etc)
                int level = Mathf.Clamp(Convert.ToInt32((long)row[1]), 1, skillData.maxLevel);
                Buff buff = new Buff((BuffSkill)skillData, level);
                // buffTimeEnd is based on Time.time, which will be
                // different when restarting a server, hence why we saved
                // them as just the remaining times. so let's convert them
                // back again.
                buff.buffTimeEnd = (float)row[2] + Time.time;
                player.buffs.Add(buff);
            }
        }
    }

    static void LoadQuests(Player player)
    {
        // load quests
        List<List<object>> table = ExecuteReader("SELECT name, killed, completed FROM character_quests WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (List<object> row in table)
        {
            string questName = (string)row[0];
            ScriptableQuest questData;
            if (ScriptableQuest.dict.TryGetValue(questName.GetStableHashCode(), out questData))
            {
                Quest quest = new Quest(questData);
                quest.killed = Convert.ToInt32((long)row[1]);
                quest.completed = ((long)row[2]) != 0; // sqlite has no bool
                player.quests.Add(quest);
            }
        }
    }

    static void LoadGuild(Player player)
    {
        // in a guild?
        string guild = (string)ExecuteScalar("SELECT guild FROM character_guild WHERE character=@character", new SqliteParameter("@character", player.name));
        if (guild != null)
        {
            // load guild info
            player.guildName = guild;
            List<List<object>> table = ExecuteReader("SELECT notice FROM guild_info WHERE name=@guild", new SqliteParameter("@guild", guild));
            if (table.Count == 1)
            {
                List<object> row = table[0];
                player.guild.notice = (string)row[0];
            }

            // load members list
            List<GuildMember> members = new List<GuildMember>();
            table = ExecuteReader("SELECT character, rank FROM character_guild WHERE guild=@guild", new SqliteParameter("@guild", player.guildName));
            foreach (List<object> row in table)
            {
                GuildMember member = new GuildMember();
                member.name = (string)row[0];
                member.rank = (GuildRank)Convert.ToInt32((long)row[1]);
                member.online = Player.onlinePlayers.ContainsKey(member.name);
                if (member.name == player.name)
                {
                    member.level = player.level;
                }
                else
                {
                    object scalar = ExecuteScalar("SELECT level FROM characters WHERE name=@character", new SqliteParameter("@character", member.name));
                    member.level = scalar != null ? Convert.ToInt32((long)scalar) : 1;
                }
                members.Add(member);
            }
            player.guild.members = members.ToArray(); // guild.AddMember each time is too slow because array resizing
        }
    }

    public static GameObject CharacterLoad(string characterName, List<Player> prefabs)
    {
        List<List<object>> table = ExecuteReader("SELECT * FROM characters WHERE name=@name AND deleted=0", new SqliteParameter("@name", characterName));
        if (table.Count == 1)
        {
            List<object> mainrow = table[0];

            // instantiate based on the class name
            string className = (string)mainrow[2];
            Player prefab = prefabs.Find(p => p.name == className);
            if (prefab != null)
            {
                GameObject go = GameObject.Instantiate(prefab.gameObject);
                Player player = go.GetComponent<Player>();

                player.name = (string)mainrow[0];
                player.account = (string)mainrow[1];
                player.className = (string)mainrow[2];
                float x = (float)mainrow[3];
                float y = (float)mainrow[4];
                float z = (float)mainrow[5];
                Vector3 position = new Vector3(x, y, z);
                player.level = Convert.ToInt32((long)mainrow[6]);
                int health = Convert.ToInt32((long)mainrow[7]);
                int mana = Convert.ToInt32((long)mainrow[8]);
                player.strength = Convert.ToInt32((long)mainrow[9]);
                player.intelligence = Convert.ToInt32((long)mainrow[10]);
                player.experience = (long)mainrow[11];
                player.skillExperience = (long)mainrow[12];
                player.gold = (long)mainrow[13];
                player.coins = (long)mainrow[14];

                // try to warp to loaded position.
                // => agent.warp is recommended over transform.position and
                //    avoids all kinds of weird bugs
                // => warping might fail if we changed the world since last save
                //    so we reset to start position if not on navmesh
                player.agent.Warp(position);
                if (!player.agent.isOnNavMesh)
                {
                    Transform start = NetworkManager.singleton.GetNearestStartPosition(position);
                    player.agent.Warp(start.position);
                    Debug.Log(player.name + " invalid position was reset");
                }

                LoadInventory(player);
                LoadEquipment(player);
                LoadSkills(player);
                LoadBuffs(player);
                LoadQuests(player);
                LoadGuild(player);

                // assign health / mana after max values were fully loaded
                // (they depend on equipment, buffs, etc.)
                player.health = health;
                player.mana = mana;

                // addon system hooks
                Utils.InvokeMany(typeof(Database), null, "CharacterLoad_", player);

                return go;
            }
            else Debug.LogError("no prefab found for class: " + className);
        }
        return null;
    }

    static void SaveInventory(Player player)
    {
        // inventory: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQuery("DELETE FROM character_inventory WHERE character=@character", new SqliteParameter("@character", player.name));
        for (int i = 0; i < player.inventory.Count; ++i)
        {
            ItemSlot slot = player.inventory[i];
            if (slot.amount > 0) // only relevant items to save queries/storage/time
                ExecuteNonQuery("INSERT INTO character_inventory VALUES (@character, @slot, @name, @amount, @petHealth, @petLevel, @petExperience)",
                                new SqliteParameter("@character", player.name),
                                new SqliteParameter("@slot", i),
                                new SqliteParameter("@name", slot.item.name),
                                new SqliteParameter("@amount", slot.amount),
                                new SqliteParameter("@petHealth", slot.item.petHealth),
                                new SqliteParameter("@petLevel", slot.item.petLevel),
                                new SqliteParameter("@petExperience", slot.item.petExperience));
        }
    }

    static void SaveEquipment(Player player)
    {
        // equipment: remove old entries first, then add all new ones
        // (we could use UPDATE where slot=... but deleting everything makes
        //  sure that there are never any ghosts)
        ExecuteNonQuery("DELETE FROM character_equipment WHERE character=@character", new SqliteParameter("@character", player.name));
        for (int i = 0; i < player.equipment.Count; ++i)
        {
            ItemSlot slot = player.equipment[i];
            if (slot.amount > 0) // only relevant equip to save queries/storage/time
                ExecuteNonQuery("INSERT INTO character_equipment VALUES (@character, @slot, @name, @amount)",
                                new SqliteParameter("@character", player.name),
                                new SqliteParameter("@slot", i),
                                new SqliteParameter("@name", slot.item.name),
                                new SqliteParameter("@amount", slot.amount));
        }
    }

    static void SaveSkills(Player player)
    {
        // skills: remove old entries first, then add all new ones
        ExecuteNonQuery("DELETE FROM character_skills WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (Skill skill in player.skills)
            if (skill.level > 0) // only learned skills to save queries/storage/time
                // castTimeEnd and cooldownEnd are based on Time.time, which
                // will be different when restarting the server, so let's
                // convert them to the remaining time for easier save & load
                // note: this does NOT work when trying to save character data shortly
                //       before closing the editor or game because Time.time is 0 then.
                ExecuteNonQuery("INSERT INTO character_skills VALUES (@character, @name, @level, @castTimeEnd, @cooldownEnd)",
                                new SqliteParameter("@character", player.name),
                                new SqliteParameter("@name", skill.name),
                                new SqliteParameter("@level", skill.level),
                                new SqliteParameter("@castTimeEnd", skill.CastTimeRemaining()),
                                new SqliteParameter("@cooldownEnd", skill.CooldownRemaining()));
    }

    static void SaveBuffs(Player player)
    {
        // buffs: remove old entries first, then add all new ones
        ExecuteNonQuery("DELETE FROM character_buffs WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (Buff buff in player.buffs)
            // buffTimeEnd is based on Time.time, which will be different when
            // restarting the server, so let's convert them to the remaining
            // time for easier save & load
            // note: this does NOT work when trying to save character data shortly
            //       before closing the editor or game because Time.time is 0 then.
            ExecuteNonQuery("INSERT INTO character_buffs VALUES (@character, @name, @level, @buffTimeEnd)",
                            new SqliteParameter("@character", player.name),
                            new SqliteParameter("@name", buff.name),
                            new SqliteParameter("@level", buff.level),
                            new SqliteParameter("@buffTimeEnd", buff.BuffTimeRemaining()));
    }

    static void SaveQuests(Player player)
    {
        // quests: remove old entries first, then add all new ones
        ExecuteNonQuery("DELETE FROM character_quests WHERE character=@character", new SqliteParameter("@character", player.name));
        foreach (Quest quest in player.quests)
            ExecuteNonQuery("INSERT INTO character_quests VALUES (@character, @name, @killed, @completed)",
                            new SqliteParameter("@character", player.name),
                            new SqliteParameter("@name", quest.name),
                            new SqliteParameter("@killed", quest.killed),
                            new SqliteParameter("@completed", Convert.ToInt32(quest.completed)));
    }

    // adds or overwrites character data in the database
    public static void CharacterSave(Player player, bool online, bool useTransaction = true)
    {
        // only use a transaction if not called within SaveMany transaction
        if (useTransaction) ExecuteNonQuery("BEGIN");

        // online status:
        //   '' if offline (if just logging out etc.)
        //   current time otherwise
        // -> this way it's fault tolerant because external applications can
        //    check if online != '' and if time difference < saveinterval
        // -> online time is useful for network zones (server<->server online
        //    checks), external websites which render dynamic maps, etc.
        // -> it uses the ISO 8601 standard format
        string onlineString = online ? DateTime.UtcNow.ToString("s") : "";

        ExecuteNonQuery("INSERT OR REPLACE INTO characters VALUES (@name, @account, @class, @x, @y, @z, @level, @health, @mana, @strength, @intelligence, @experience, @skillExperience, @gold, @coins, @online, 0)",
                        new SqliteParameter("@name", player.name),
                        new SqliteParameter("@account", player.account),
                        new SqliteParameter("@class", player.className),
                        new SqliteParameter("@x", player.transform.position.x),
                        new SqliteParameter("@y", player.transform.position.y),
                        new SqliteParameter("@z", player.transform.position.z),
                        new SqliteParameter("@level", player.level),
                        new SqliteParameter("@health", player.health),
                        new SqliteParameter("@mana", player.mana),
                        new SqliteParameter("@strength", player.strength),
                        new SqliteParameter("@intelligence", player.intelligence),
                        new SqliteParameter("@experience", player.experience),
                        new SqliteParameter("@skillExperience", player.skillExperience),
                        new SqliteParameter("@gold", player.gold),
                        new SqliteParameter("@coins", player.coins),
                        new SqliteParameter("@online", onlineString));

        SaveInventory(player);
        SaveEquipment(player);
        SaveSkills(player);
        SaveBuffs(player);
        SaveQuests(player);

        // addon system hooks
        Utils.InvokeMany(typeof(Database), null, "CharacterSave_", player);

        if (useTransaction) ExecuteNonQuery("END");
    }

    // save multiple characters at once (useful for ultra fast transactions)
    public static void CharacterSaveMany(List<Player> players, bool online = true)
    {
        ExecuteNonQuery("BEGIN"); // transaction for performance
        foreach (Player player in players)
            CharacterSave(player, online, false);
        ExecuteNonQuery("END");
    }

    // guilds //////////////////////////////////////////////////////////////////
    public static bool GuildExists(string guild)
    {
        return ((long)ExecuteScalar("SELECT Count(*) FROM guild_info WHERE name=@name", new SqliteParameter("@name", guild))) == 1;
    }

    public static void SaveGuild(string guild, string notice, List<GuildMember> members)
    {
        ExecuteNonQuery("BEGIN"); // transaction for performance

        // guild info
        ExecuteNonQuery("INSERT OR REPLACE INTO guild_info VALUES (@guild, @notice)",
                        new SqliteParameter("@guild", guild),
                        new SqliteParameter("@notice", notice));

        // members list
        ExecuteNonQuery("DELETE FROM character_guild WHERE guild=@guild", new SqliteParameter("@guild", guild));
        foreach (GuildMember member in members)
        {
            ExecuteNonQuery("INSERT INTO character_guild VALUES (@character, @guild, @rank)",
                            new SqliteParameter("@character", member.name),
                            new SqliteParameter("@guild", guild),
                            new SqliteParameter("@rank", member.rank));
        }

        ExecuteNonQuery("END");
    }

    public static void RemoveGuild(string guild)
    {
        ExecuteNonQuery("BEGIN"); // transaction for performance
        ExecuteNonQuery("DELETE FROM guild_info WHERE name=@name", new SqliteParameter("@name", guild));
        ExecuteNonQuery("DELETE FROM character_guild WHERE guild=@guild", new SqliteParameter("@guild", guild));
        ExecuteNonQuery("END");
    }

    // item mall ///////////////////////////////////////////////////////////////
    public static List<long> GrabCharacterOrders(string characterName)
    {
        // grab new orders from the database and delete them immediately
        //
        // note: this requires an orderid if we want someone else to write to
        // the database too. otherwise deleting would delete all the new ones or
        // updating would update all the new ones. especially in sqlite.
        //
        // note: we could just delete processed orders, but keeping them in the
        // database is easier for debugging / support.
        List<long> result = new List<long>();
        List<List<object>> table = ExecuteReader("SELECT orderid, coins FROM character_orders WHERE character=@character AND processed=0", new SqliteParameter("@character", characterName));
        foreach (List<object> row in table)
        {
            result.Add((long)row[1]);
            ExecuteNonQuery("UPDATE character_orders SET processed=1 WHERE orderid=@orderid", new SqliteParameter("@orderid", (long)row[0]));
        }
        return result;
    }
}
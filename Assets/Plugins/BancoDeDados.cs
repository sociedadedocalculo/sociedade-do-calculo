using System.Data;
using Mono.Data.SqliteClient;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour {

// caminho para o arquivo do banco

string urlDataBase = "URI=file:MasterSQLite.db";

void Iniciar()
{

IDbConnection _connection = new SqliteConnection(urlDataBase);

IDbCommand _command = _connection .CreateCommand();

string sql;

_connection .Open();

// assim só criaremos a tabela uma vez

string sql = "CREATE TABLE IF NOT EXISTS highscores (name VARCHAR(20), score INT)";

_command.CommandText = sql;

_command.ExecuteNonQuery();

}

public void Inserir()

{

string sql = "INSERT INTO highscores (name, score) VALUES (‘Me’, 3000)";

_command.CommandText = sql;

_command.ExecuteNonQuery();

}

void Recuperar()

{

string sqlQuery = "SELECT value,name, randomSequence " + "FROM PlaceSequence";

dbcmd.CommandText = sqlQuery;

IDataReader reader = dbcmd.ExecuteReader();

while (reader.Read())

{

int value = reader.GetInt32(0);

string name = reader.GetString(1);

int rand = reader.GetInt32(2);

Debug.Log( "value= "+value+" name ="+name+" random ="+ rand);

}

}
}
using Microsoft.Data.Sqlite;

namespace roshaikinbot;

public class Storage
{
    private SqliteConnection _connection;
    
    public Storage()
    {
        _connection = new SqliteConnection("Data Source = db.sqlite");
        _connection.Open();

        ExecuteNonQuery("""
                        CREATE TABLE IF NOT EXISTS drunken_beers(
                            id INTEGER PRIMARY KEY AUTOINCREMENT, 
                            amountMl REAL, 
                            user_id INTEGER NOT NULL,
                            chat_id INTEGER NOT NULL
                        );
                        """);
    }

    private void ExecuteNonQuery(string query)
    {
        var command = _connection.CreateCommand();
        command.CommandText = query;

        command.ExecuteNonQuery();
    }

    private SqliteDataReader ExecuteReader(string query)
    {
        var command = _connection.CreateCommand();
        command.CommandText = query;
        return command.ExecuteReader();
    }

    private T ExecuteScalar<T>(string query)
    {
        var command = _connection.CreateCommand();
        command.CommandText = query;
        return (T)command.ExecuteScalar()!;
    }
    
    public void AddDrunkenBeer(double amountMl, long userId, long chatId)
    {
        ExecuteNonQuery($"""
                         INSERT INTO drunken_beers (amountMl, user_id, chat_id) 
                         VALUES ({amountMl}, {userId}, {chatId});
                         """);
        
        Console.WriteLine($"Added {amountMl}ml for user {userId} in chat {chatId}");
    }

    public double GetDrunkenBeer(long userId, long chatId)
    {
        var reader = ExecuteReader($"""
                                   SELECT amountMl
                                   FROM drunken_beers
                                   WHERE user_id = {userId} AND chat_id = {chatId};
                                   """);
        double amountMlTotal = 0;
        while (reader.Read())
        {
            amountMlTotal += reader.GetDouble(0);
        }

        return amountMlTotal;
    }

    public void CleanupDrunkenBeer(long userId, long chatId)
    {
        ExecuteNonQuery($"""
                         DELETE FROM drunken_beers
                         WHERE user_id = {userId} AND chat_id = {chatId};
                         """);
    }
}
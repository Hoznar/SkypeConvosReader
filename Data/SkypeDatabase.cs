using Microsoft.Data.Sqlite;
using SkypeConvosReader.Models;

namespace SkypeConvosReader.Data;

public class SkypeDatabase {
    private readonly string _databasePath;
    
    private const string ContactColumns = """
                                          id,
                                          skypename,
                                          fullname,
                                          birthday,
                                          gender,
                                          country,
                                          province,
                                          city,
                                          phone_mobile,
                                          homepage,
                                          about,
                                          avatar_image,
                                          mood_text
                                          """;

    public SkypeDatabase(string databasePath) {
        _databasePath = databasePath;
    }

    private SqliteConnection CreateConnection() {
        return new SqliteConnection($"Data Source={_databasePath};Mode=ReadOnly");
    }

    public List<Account> GetAccounts() {
        var accounts = new List<Account>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
                              SELECT {ContactColumns}
                              FROM Accounts
                              ORDER BY id
                              """;

        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            var contact = ReadContact(reader);
            accounts.Add(new Account {
                Id = reader.GetInt32(0),
                SkypeName = contact.SkypeName,
                FullName = contact.FullName,
                Contact = contact
            });
        }
        return accounts;
    }

    public List<Conversation> GetConversations() {
        var conversations = new List<Conversation>();
        
        using var connection = CreateConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT
                                  c.id,
                                  c.identity,
                                  c.type,
                                  c.displayname,
                                  MAX(m.timestamp) AS last_message_timestamp,
                                  MIN(m.timestamp) AS first_message_timestamp,
                                  COUNT(m.id) AS message_count
                              FROM Conversations c
                              JOIN Messages m
                              ON c.id = m.convo_id
                              WHERE m.body_xml IS NOT NULL
                              GROUP BY c.id
                              ORDER BY c.inbox_timestamp DESC
                              """;
        
        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            conversations.Add(new Conversation {
                Id = reader.GetInt32(0),
                Identity = reader.IsDBNull(1) ? null : reader.GetString(1),
                Type = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                LastMessageDate = TimestampToDateTime(reader.IsDBNull(4) ? 0 : reader.GetInt64(4)),
                FirstMessageDate = TimestampToDateTime(reader.IsDBNull(5) ? 0 : reader.GetInt64(5)),
                MessageCount = reader.GetInt32(6),
            });
        }
        return conversations;
    }
    
    public List<Contact> GetContacts() {
        var contacts = new List<Contact>();
        using var connection = CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
                              SELECT {ContactColumns}
                              FROM Contacts
                              """;

        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            contacts.Add(ReadContact(reader));
        }
        return contacts;
    }
    
    public List<Contact> GetParticipants(int convoId) {
        var participants = new List<Contact>();
        
        using var connection = CreateConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT
                                  c.id,
                                  c.skypename,
                                  c.fullname,
                                  c.birthday,
                                  c.gender,
                                  c.country,
                                  c.province,
                                  c.city,
                                  c.phone_mobile,
                                  c.homepage,
                                  c.about,
                                  c.avatar_image,
                                  c.mood_text
                              FROM Participants p
                              JOIN Contacts c ON p.identity = c.skypename
                              WHERE p.convo_id = @convoId
                              """;
        command.Parameters.AddWithValue("@convoId", convoId);
        
        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            participants.Add(new Contact {
                Id = reader.GetInt32(0),
                SkypeName = reader.IsDBNull(1) ? null : reader.GetString(1),
                FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
                Birthday = TimestampToDateTime(reader.IsDBNull(3) ? 0 : reader.GetInt64(3)),
                Gender = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Country = reader.IsDBNull(5) ? null : reader.GetString(5),
                Province = reader.IsDBNull(6) ? null : reader.GetString(6),
                City = reader.IsDBNull(7) ? null : reader.GetString(7),
                Phone =  reader.IsDBNull(8) ? null : reader.GetString(8),
                Website = reader.IsDBNull(9) ? null : reader.GetString(9),
                About = reader.IsDBNull(10) ? null : reader.GetString(10),
                AvatarImage = reader.IsDBNull(11) ? null : (byte[])reader[11],
                Mood =  reader.IsDBNull(12) ? null : reader.GetString(12)
            });
        }
        return participants;
    }
    
    public List<string> GetParticipantsNames(int convoId) {
        var participants = new List<string>();
        
        using var connection = CreateConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT identity
                              FROM Participants
                              WHERE convo_id = @convoId
                              """;
        command.Parameters.AddWithValue("@convoId", convoId);
        
        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            participants.Add(reader.IsDBNull(0) ? "" : reader.GetString(0));
        }
        return participants;
    }

    public List<Message> GetMessages(int conversationId, int limit, int offset,  bool descending = true) {
        var messages = new List<Message>();
        
        using var connection = CreateConnection();
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = $"""
                              SELECT
                                  id,
                                  convo_id,
                                  author,
                                  from_dispname,
                                  timestamp,
                                  type,
                                  body_xml,
                                  identities
                              FROM Messages
                              WHERE convo_id = @conversationId
                              AND (
                                  body_xml IS NOT NULL
                                  OR type IN (10, 13)
                              )
                              ORDER BY timestamp {(descending ? "DESC" : "ASC")}, id DESC
                              LIMIT @limit OFFSET @offset
                              """;
        command.Parameters.AddWithValue("@conversationId", conversationId);
        command.Parameters.AddWithValue("@limit", limit);
        command.Parameters.AddWithValue("@offset", offset);
        
        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            messages.Add(new Message {
                Id = reader.GetInt32(0),
                ConversationId = reader.GetInt32(1),
                Author = reader.IsDBNull(2) ? null : reader.GetString(2),
                FromDisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                Timestamp = TimestampToDateTime(reader.IsDBNull(4) ? 0 : reader.GetInt64(4)),
                Type = reader.GetInt32(5),
                BodyXml = reader.IsDBNull(6) ? null : reader.GetString(6),
                Identities = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }
        return messages;
    }
    
    private Contact ReadContact(SqliteDataReader reader) {
        return new Contact {
            Id = reader.GetInt32(0),
            SkypeName = reader.IsDBNull(1) ? null : reader.GetString(1),
            FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
            Birthday = TimestampToDateTime(reader.IsDBNull(3) ? 0 : reader.GetInt64(3)),
            Gender = reader.IsDBNull(4) ? null : reader.GetInt32(4),
            Country = reader.IsDBNull(5) ? null : reader.GetString(5),
            Province = reader.IsDBNull(6) ? null : reader.GetString(6),
            City = reader.IsDBNull(7) ? null : reader.GetString(7),
            Phone = reader.IsDBNull(8) ? null : reader.GetString(8),
            Website = reader.IsDBNull(9) ? null : reader.GetString(9),
            About = reader.IsDBNull(10) ? null : reader.GetString(10),
            AvatarImage = reader.IsDBNull(11) ? null : (byte[])reader[11],
            Mood = reader.IsDBNull(12) ? null : reader.GetString(12)
        };
    }

    private DateTime TimestampToDateTime(long timestamp) {
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp).ToLocalTime();
    }
}